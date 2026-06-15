namespace DonationWorker.Domain.Shared.Interfaces;

public interface ICryptoService
{
    bool VerifyPassword(string password, string HashPassword);
    string Encrypt(string value);
    string Decrypt(string value);
}