using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using DonationWorker.Domain.Enums;
using DonationWorker.Domain.Shared.Interfaces;
using System.Text.Json;

namespace DonationWorker.Infrastructure.Services.AuditLog
{
    public class AuditLogService : IAuditLogService
    {
        private readonly IDynamoDBContext _context;
        private readonly IBaseLogger<AuditLogService> _logger;
        public AuditLogService(IAmazonDynamoDB dynamoDbClient, IBaseLogger<AuditLogService> logger)
        {
            _context = new DynamoDBContext(dynamoDbClient);
            _logger = logger;
        }
        public async Task SaveLogAsync(string entityType, string entityId, string operation, string user, object data, string service)
        {
            try
            {
                _logger.LogInformation("Preparando audit log: {EntityType} {EntityId} {Operation} {User}", BaseLogType.LOG,
                    new { EntityType = entityType, EntityId = entityId, Operation = operation, User = user });

                var entry = new AuditLog
                {
                    PK = $"ENTITY#{entityType.ToUpper()}#{entityId}",
                    SK = $"TS#{DateTime.UtcNow:O}",
                    ResourceId = entityId,
                    ServiceName = service,
                    Operation = operation,
                    ChangedBy = user,
                    Payload = JsonSerializer.Serialize(data),
                    // Define expiração para 1 ano (exemplo)
                    ExpirationTime = DateTimeOffset.UtcNow.AddYears(1).ToUnixTimeSeconds()
                };

                _logger.LogInformation("Salvando audit log: {EntityType} {EntityId} {Operation} {User}", BaseLogType.LOG,
                    new { EntityType = entityType, EntityId = entityId, Operation = operation, User = user });

                await _context.SaveAsync(entry);

            }
            catch (Exception ex)
            {
                _logger.LogError("Erro ao salvar audit log: {EntityType} {EntityId}", BaseLogType.LOG, ex, new { EntityType = entityType, EntityId = entityId });
                throw;
            }
        }

        public async Task SaveRawLogAsync(AuditLog log)
        {
            try
            {
                _logger.LogInformation("Salvando raw audit log: {ResourceId} {Operation} {User}", BaseLogType.LOG,
                    new { ResourceId = log.ResourceId, Operation = log.Operation, User = log.ChangedBy });

                await _context.SaveAsync(log);
            }
            catch (Exception ex)
            {
                _logger.LogError("Erro ao salvar raw audit log: {ResourceId}", BaseLogType.LOG, ex, new { ResourceId = log.ResourceId });
                throw;
            }
        }
    }
}
