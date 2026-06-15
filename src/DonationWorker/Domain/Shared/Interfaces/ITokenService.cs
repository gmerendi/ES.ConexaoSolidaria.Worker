namespace DonationWorker.Domain.Shared.Interfaces;

public interface ITokenService
{
    TimeSpan GetTokenTimeToExpire(string token);
}   