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
        _logger.LogInformation("SavingChanges disparado - Capturando dados de auditoria", BaseLogType.LOG, eventData);
        CaptureChanges(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("SavingChangesAsync disparado - Capturando dados de auditoria", BaseLogType.LOG, eventData);
        CaptureChanges(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void CaptureChanges(DbContext? context)
    {
        _logger.LogInformation("CaptureChanges disparado - Capturando dados de auditoria", BaseLogType.LOG, context);
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

            // Montagem das chaves padrão DynamoDB
            var audit = new AuditLog
            {
                PK = $"ENTITY#{tableName}#{entity.Guid}",
                SK = $"TS#{DateTime.UtcNow:O}",
                ResourceId = entity.Guid.ToString(),
                ServiceName = "CS-DONATIONWORKER-API", 
                Operation = entry.State.ToString().ToUpper(),
                ExpirationTime = DateTimeOffset.UtcNow.AddYears(1).ToUnixTimeSeconds()
            };

            // Lógica de Diff/Payload
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
                auditData = entry.Properties.ToDictionary(p => p.Metadata.Name, p => Normalize(p.CurrentValue));
            }

            audit.Payload = JsonSerializer.Serialize(auditData);
            list.Add(audit);
        }
        var count = list.Count;
        _auditEntries.Value = list;
        _logger.LogInformation("AuditEntries para ser gravadas: " + count.ToString(), BaseLogType.LOG, list);
    }

    // 2. PERSISTÊNCIA: Após o sucesso no banco relacional
    public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("SavedChangesAsync disparado - Capturando dados de auditoria", BaseLogType.LOG, eventData);
        await PersistAuditAsync();
        return result;
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        _logger.LogInformation("SavedChanges disparado - Capturando dados de auditoria", BaseLogType.LOG, eventData);
        PersistAuditAsync().GetAwaiter().GetResult();
        return base.SavedChanges(eventData, result);
    }

    private async Task PersistAuditAsync()
    {
        _logger.LogInformation("PersistAuditAsync disparado - Capturando dados de auditoria", BaseLogType.LOG, null);
        var entries = _auditEntries.Value;
        if (entries == null || !entries.Any()) return;

        using var scope = _serviceProvider.CreateScope();

        var auditRepository = scope.ServiceProvider.GetRequiredService<IAuditLogService>();
        var userContext = scope.ServiceProvider.GetRequiredService<IUserContext>();
        //var httpContext = scope.ServiceProvider.GetService<IHttpContextAccessor>();

        var currentUser = userContext.GetUser()?.Email ?? "Worker";
        var ip = "Internal";

        foreach (var entry in entries)
        {
            try
            {
                entry.ChangedBy = currentUser;
                entry.IpAddress = ip;


                _logger.LogInformation("Chamando serviço de audit log", BaseLogType.LOG, entry);
                await auditRepository.SaveRawLogAsync(entry);
            }
            catch (Exception ex) {
                _logger.LogInformation("Falha ao chamar serviço audit log" + ex.Message, BaseLogType.LOG, entry);
            }
        }

        _auditEntries.Value = null;
    }

    private object? Normalize(object? value) => value is DateTime dt ? dt.ToUniversalTime() : value;
}