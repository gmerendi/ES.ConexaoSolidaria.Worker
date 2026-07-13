using DonationWorker.Domain.Enums;
using DonationWorker.Domain.Shared.Interfaces;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DonationWorker.Infrastructure.Services.Logging;

/// <summary>
/// Logger estruturado — suporta message templates estilo Serilog.
/// A persistência é delegada ao <see cref="IBaseLoggerDbWriter"/>, tornando
/// o mecanismo de storage completamente intercambiável.
/// </summary>
public class BaseLogger<T> : IBaseLogger<T>
{
    protected readonly ILogger<T> _logger;
    protected readonly ICorrelationIdGenerator _correlationIdGenerator;
    protected readonly IConfiguration _configuration;
    protected readonly IBaseLoggerDbWriter _baseLoggerDbWriter;

    private static readonly Regex TemplateRegex =
        new(@"\{(\w+)(?::[^}]*)?\}", RegexOptions.Compiled);

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = false,
        ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public BaseLogger(
        ILogger<T> logger,
        ICorrelationIdGenerator correlationIdGenerator,
        IConfiguration configuration,
        IBaseLoggerDbWriter baseLoggerDbWriter)
    {
        _logger = logger;
        _correlationIdGenerator = correlationIdGenerator;
        _configuration = configuration;
        _baseLoggerDbWriter = baseLoggerDbWriter;
    }

    // ══════════════════════════════════════════════════════════════════════
    // Interface pública
    // ══════════════════════════════════════════════════════════════════════

    public void LogInformation(string template, BaseLogType type,
        object? properties = null, string? correlationId = null)
    {
        if (_configuration["CustomLogging:LogInfo"] != "True") return;
        var id = ResolveCorrelationId(correlationId);
        var props = ExtrairPropriedades(properties);
        var msg = RenderTemplate(template, props);

        _logger.LogInformation("[{CorrelationId}] [Type:{Type}] {Message} {@Properties}",
            id, type, msg, props);

        PersistirAsync("Information", type, template, msg, props, id);
    }

    public void LogWarning(string template, BaseLogType type,
        object? properties = null, string? correlationId = null)
    {
        if (_configuration["CustomLogging:LogWarning"] != "True") return;
        var id = ResolveCorrelationId(correlationId);
        var props = ExtrairPropriedades(properties);
        var msg = RenderTemplate(template, props);

        _logger.LogWarning("[{CorrelationId}] [Type:{Type}] {Message} {@Properties}",
            id, type, msg, props);

        PersistirAsync("Warning", type, template, msg, props, id);
    }

    public void LogError(string template, BaseLogType type,
        object? properties = null, string? correlationId = null)
    {
        if (_configuration["CustomLogging:LogError"] != "True") return;
        var id = ResolveCorrelationId(correlationId);
        var props = ExtrairPropriedades(properties);
        var msg = RenderTemplate(template, props);

        _logger.LogError("[{CorrelationId}] [Type:{Type}] {Message} {@Properties}",
            id, type, msg, props);

        PersistirAsync("Error", type, template, msg, props, id);
    }

    public void LogError(string template, BaseLogType type,
        Exception exception, object? properties = null, string? correlationId = null)
    {
        if (_configuration["CustomLogging:LogError"] != "True") return;
        var id = ResolveCorrelationId(correlationId);
        var props = ExtrairPropriedades(properties);

        // Enriquece automaticamente com dados da exceção
        props["ExceptionType"] = exception.GetType().FullName ?? "Unknown";
        props["ExceptionMsg"] = exception.Message;
        props["StackTrace"] = exception.StackTrace ?? "N/A";

        var msg = RenderTemplate(template, props);

        _logger.LogError(exception, "[{CorrelationId}] [Type:{Type}] {Message} {@Properties}",
            id, type, msg, props);

        PersistirAsync("Error", type, template, msg, props, id);
    }

    // ══════════════════════════════════════════════════════════════════════
    // Persistência — delega ao IBaseLoggerDbWriter
    // ══════════════════════════════════════════════════════════════════════

    private void PersistirAsync(
        string logLevel,
        BaseLogType type,
        string template,
        string rendered,
        Dictionary<string, object?> properties,
        string correlationId)
    {
        if (type != BaseLogType.EVENT && _configuration["CustomLogging:SendLogToDB"] != "True")
            return;

        var entry = new LogEntry(
            CorrelationId: correlationId,
            Timestamp: DateTime.UtcNow.ToString("o"),
            Type: (int)type,
            LogLevel: logLevel,
            Caller: typeof(T).FullName ?? "Unknown",
            Template: template,
            Message: rendered,
            PropertiesJson: properties.Count > 0
                ? JsonSerializer.Serialize(properties, JsonOpts)
                : "{}");

        _ = _baseLoggerDbWriter.WriteAsync(entry);
    }

    // ══════════════════════════════════════════════════════════════════════
    // Template rendering
    // ══════════════════════════════════════════════════════════════════════

    private static string RenderTemplate(string template, Dictionary<string, object?> props) =>
        TemplateRegex.Replace(template, m =>
            props.TryGetValue(m.Groups[1].Value, out var val)
                ? val?.ToString() ?? "null"
                : m.Value);

    private static Dictionary<string, object?> ExtrairPropriedades(object? properties)
    {
        if (properties is null)
            return new Dictionary<string, object?>();

        // Tipos primitivos, strings e exceptions não devem ser tratados como properties
        if (properties is string or Exception or ValueType)
            return new Dictionary<string, object?>();

        if (properties is Dictionary<string, object?> dict)
            return dict;

        if (properties is IDictionary<string, object> idict)
            return idict.ToDictionary(kv => kv.Key, kv => (object?)kv.Value);

        // Objeto anônimo ou POCO — ignora propriedades indexadas (ex: string.Chars[i])
        return properties
            .GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetIndexParameters().Length == 0)
            .ToDictionary(p => p.Name, p => p.GetValue(properties));
    }

    // ══════════════════════════════════════════════════════════════════════
    // CorrelationId
    // ══════════════════════════════════════════════════════════════════════

    private string ResolveCorrelationId(string? passedCorrelationId)
    {
        if (!string.IsNullOrWhiteSpace(passedCorrelationId))
        {
            _correlationIdGenerator.Set(passedCorrelationId);
            return passedCorrelationId;
        }

        var current = _correlationIdGenerator.Get();
        if (string.IsNullOrWhiteSpace(current))
        {
            current = Guid.NewGuid().ToString();
            _correlationIdGenerator.Set(current);
        }

        return current;
    }
}