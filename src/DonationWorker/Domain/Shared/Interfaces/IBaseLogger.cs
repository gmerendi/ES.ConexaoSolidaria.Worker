using DonationWorker.Domain.Enums;

namespace DonationWorker.Domain.Shared.Interfaces
{
    public interface IBaseLogger<T>
    {
        void LogInformation(string message, BaseLogType type, object? data, string? correlationId = null);
        void LogError(string message, BaseLogType type, object? data, string? correlationId = null);
        void LogWarning(string message, BaseLogType type, object? data, string? correlationId = null);
    }
}
