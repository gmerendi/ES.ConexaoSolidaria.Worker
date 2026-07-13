using DonationWorker.Domain.Enums;

namespace DonationWorker.Domain.Shared.Interfaces;

/// <summary>
/// Logger estruturado — suporta message templates estilo Serilog.
/// As propriedades nomeadas no template são extraídas e armazenadas
/// como campos estruturados no DynamoDB.
///
/// Uso:
///   logger.LogInformation(
///       "Login de {Email} a partir de {Ip}",
///       BaseLogType.EVENT,
///       new { Email = "user@test.com", Ip = "10.0.0.1" });
/// </summary>
public interface IBaseLogger<T>
{
    // ── Structured (preferred) ────────────────────────────────────────────
    void LogInformation(string template, BaseLogType type, object? properties = null, string? correlationId = null);
    void LogWarning(string template, BaseLogType type, object? properties = null, string? correlationId = null);
    void LogError(string template, BaseLogType type, object? properties = null, string? correlationId = null);

    // ── Exception overload ────────────────────────────────────────────────
    void LogError(string template, BaseLogType type, Exception exception, object? properties = null, string? correlationId = null);
}