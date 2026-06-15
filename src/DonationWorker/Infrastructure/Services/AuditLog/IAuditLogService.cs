namespace DonationWorker.Infrastructure.Services.AuditLog
{
    public interface IAuditLogService
    {
        Task SaveLogAsync(string entityType, string entityGuid, string action, string user, object data, string service);
        Task SaveRawLogAsync(AuditLog log);
    }
}
