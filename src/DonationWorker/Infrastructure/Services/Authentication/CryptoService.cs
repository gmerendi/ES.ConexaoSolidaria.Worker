using DonationWorker.Domain.Shared.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace DonationWorker.Infrastructure.Services.Security;

public class CryptoService : ICryptoService
{
    private readonly string _chaveAes;

    public CryptoService(IConfiguration configuration)
    {
        _chaveAes = configuration["AES:KEY"]!;
    }



    // ── BCrypt — verificação de senha ─────────────────────────────────────────
    public bool VerifyPassword(string password, string dbHashPassword)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(dbHashPassword))
            return false;

        // O BCrypt descriptografa o salt de dentro do próprio hash e faz a checagem com segurança
        return BCrypt.Net.BCrypt.Verify(password, dbHashPassword);
    }



    // ── AES — encriptação/decriptação de CPF ──────────────────────────────────
    public string Encrypt(string value)
    {
        using var aes = Aes.Create();
        aes.Key = Convert.FromBase64String(_chaveAes);
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var bytes = Encoding.UTF8.GetBytes(value);
        var cifrado = encryptor.TransformFinalBlock(bytes, 0, bytes.Length);

        // IV + cifrado juntos em Base64
        var resultado = new byte[aes.IV.Length + cifrado.Length];
        aes.IV.CopyTo(resultado, 0);
        cifrado.CopyTo(resultado, aes.IV.Length);

        return Convert.ToBase64String(resultado);
    }

    public string Decrypt(string value)
    {
        using var aes = Aes.Create();
        aes.Key = Convert.FromBase64String(_chaveAes);

        var dadosCompletos = Convert.FromBase64String(value);
        aes.IV = dadosCompletos[..16];  // primeiros 16 bytes são o IV

        using var decryptor = aes.CreateDecryptor();
        var dadosCifrados = dadosCompletos[16..];
        var resultado = decryptor.TransformFinalBlock(dadosCifrados, 0, dadosCifrados.Length);

        return Encoding.UTF8.GetString(resultado);
    }
}