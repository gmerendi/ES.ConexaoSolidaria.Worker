namespace DonationWorker.Infrastructure.Services.Logging;

/// <summary>
/// Contrato para persistência de logs.
/// </summary>
public interface IBaseLoggerDbWriter
{
    Task WriteAsync(LogEntry entry);
}

/// <summary>
/// Representa um registro de log estruturado pronto para persistência.
/// </summary>
public record LogEntry(
    string CorrelationId,
    string Timestamp,
    int Type,
    string LogLevel,
    string Caller,
    string Template,
    string Message,
    string PropertiesJson);   // JSON serializado das propriedades nomeadas