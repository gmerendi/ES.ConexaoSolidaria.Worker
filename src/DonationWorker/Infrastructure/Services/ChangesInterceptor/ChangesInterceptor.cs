using DonationWorker.Domain.Enums;
using DonationWorker.Domain.Shared.Entity;
using DonationWorker.Domain.Shared.Interfaces;
using DonationWorker.Infrastructure.Services.AuditLog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Text.Json;

public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IBaseLogger<AuditInterceptor> _logger;
    private static readonly AsyncLocal<List<AuditLog>> _auditEntries = new();

    public AuditInterceptor(IServiceProvider serviceProvider, IBaseLogger<AuditInterceptor> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    // 1. CAPTURA: Antes de salvar (ainda temos acesso aos valores originais)
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        _logger.LogInformation("SavingChanges disparado - capturando dados de auditoria.", BaseLogType.LOG);
        CaptureChanges(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("SavingChangesAsync disparado - capturando dados de auditoria.", BaseLogType.LOG);
        CaptureChanges(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void CaptureChanges(DbContext? context)
    {
        if (context == null) return;

        var entries = context.ChangeTracker.Entries()
            .Where(e => e.Entity is EntityBase &&
                        (e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted))
            .ToList();

        var list = new List<AuditLog>();

        foreach (var entry in entries)
        {
            var entity = (EntityBase)entry.Entity;
            var tableName = entry.Metadata.GetTableName()?.ToUpper() ?? "UNKNOWN";

            var audit = new AuditLog
            {
                PK = $"ENTITY#{tableName}#{entity.Guid}",
                SK = $"TS#{DateTime.UtcNow:O}",
                ResourceId = entity.Guid.ToString(),
                ServiceName = "CS-CAMPANHAS-API",
                Operation = entry.State.ToString().ToUpper(),
                ExpirationTime = DateTimeOffset.UtcNow.AddYears(1).ToUnixTimeSeconds()
            };

            object? auditData = null;
            if (entry.State == EntityState.Modified)
            {
                auditData = entry.Properties
                    .Where(p => p.IsModified && !Equals(p.OriginalValue, p.CurrentValue))
                    .ToDictionary(p => p.Metadata.Name, p => new {
                        old = Normalize(p.OriginalValue),
                        @new = Normalize(p.CurrentValue)
                    });
            }
            else
            {
                auditData = entry.Properties
                    .ToDictionary(p => p.Metadata.Name, p => Normalize(p.CurrentValue));
            }

            audit.Payload = JsonSerializer.Serialize(auditData);
            list.Add(audit);
        }

        _auditEntries.Value = list;
        _logger.LogInformation("{Count} entradas de auditoria capturadas.", BaseLogType.LOG, new { Count = list.Count });
    }

    // 2. PERSISTÊNCIA: Após o sucesso no banco relacional
    public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("SavedChangesAsync disparado - persistindo auditoria.", BaseLogType.LOG);
        await PersistAuditAsync();
        return result;
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        _logger.LogInformation("SavedChanges disparado - persistindo auditoria.", BaseLogType.LOG);
        PersistAuditAsync().GetAwaiter().GetResult();
        return base.SavedChanges(eventData, result);
    }

    private async Task PersistAuditAsync()
    {
        var entries = _auditEntries.Value;
        if (entries == null || !entries.Any()) return;

        using var scope = _serviceProvider.CreateScope();

        var auditRepository = scope.ServiceProvider.GetRequiredService<IAuditLogService>();
        var userContext = scope.ServiceProvider.GetRequiredService<IUserContext>();
        var httpContext = scope.ServiceProvider.GetService<IHttpContextAccessor>();

        var currentUser = userContext.GetUser()?.Email ?? "System";
        var ip = httpContext?.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Internal";

        foreach (var entry in entries)
        {
            try
            {
                entry.ChangedBy = currentUser;
                entry.IpAddress = ip;

                _logger.LogInformation("Persistindo entrada de auditoria: {ResourceId} {Operation}", BaseLogType.LOG,
                    new { ResourceId = entry.ResourceId, Operation = entry.Operation });

                await auditRepository.SaveRawLogAsync(entry);
            }
            catch (Exception ex)
            {
                _logger.LogError("Falha ao persistir entrada de auditoria: {ResourceId}", BaseLogType.LOG, ex,
                    new { ResourceId = entry.ResourceId });
            }
        }

        _auditEntries.Value = null;
    }

    private object? Normalize(object? value) => value is DateTime dt ? dt.ToUniversalTime() : value;
}