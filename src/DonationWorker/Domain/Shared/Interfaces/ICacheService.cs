namespace DonationWorker.Domain.Shared.Interfaces
{
    public interface ICacheService
    {
        Task SetAsync<T>(string key, T value, TimeSpan expiration);
        Task<T?> GetAsync<T>(string key);
        Task RemoveAsync(string key);
        Task RemoveByPrefixAsync(string prefixo);

        Task SetBlacklistAsync(string token, TimeSpan expiration, CancellationToken ct = default);
        Task<bool> IsBlacklistedAsync(string token, CancellationToken ct = default);
    }
}
