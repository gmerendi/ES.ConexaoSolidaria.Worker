using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;

namespace DonationWorker.Infrastructure.Services.Logging;

/// <summary>
/// Implementação de <see cref="IBaseLoggerDbWriter"/> que persiste logs no DynamoDB.
/// Para trocar de banco (ex: PostgreSQL, Elasticsearch), basta modificar
/// a implementação dessa classe — o BaseLogger não muda.
/// </summary>
public class BaseLoggerDbWriter : IBaseLoggerDbWriter
{
    private readonly IAmazonDynamoDB _dynamoDb;
    private readonly ILogger<BaseLoggerDbWriter> _logger;
    private readonly string _tableName;
    private readonly Table _tabelaAppLog;

    public BaseLoggerDbWriter(
        IAmazonDynamoDB dynamoDb,
        IConfiguration configuration,
        ILogger<BaseLoggerDbWriter> logger)
    {
        _dynamoDb = dynamoDb;
        _logger = logger;
        var nomeTabela = configuration["DynamoDB:AppLogTable"] ?? "cs-app-log";
        _tabelaAppLog = new TableBuilder(dynamoDb, nomeTabela)
        .AddHashKey("CorrelationId", DynamoDBEntryType.String)
        .AddRangeKey("Timestamp", DynamoDBEntryType.String)
        .Build();
    }

    public Task WriteAsync(LogEntry entry)
    {
        // Fire-and-forget — não bloqueia o fluxo principal
        Task.Run(async () =>
        {
            try
            {
                var tabela = _tabelaAppLog;

                var doc = new Document
                {
                    ["CorrelationId"] = entry.CorrelationId,
                    ["Timestamp"] = entry.Timestamp,
                    ["Type"] = entry.Type,
                    ["LogLevel"] = entry.LogLevel,
                    ["Caller"] = entry.Caller,
                    ["Template"] = entry.Template,
                    ["Message"] = entry.Message,
                    ["Properties"] = entry.PropertiesJson,
                };

                await tabela.PutItemAsync(doc);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Erro ao persistir log no DynamoDB.");
            }
        });

        return Task.CompletedTask;
    }
}