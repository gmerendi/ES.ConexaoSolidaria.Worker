using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using DonationWorker.Domain.Enums;
using DonationWorker.Domain.Shared.Interfaces;
using System.Text.Json;

namespace DonationWorker.Infrastructure.Services.Logging
{
    public class BaseLogger<T> : IBaseLogger<T>
    {
        protected readonly ILogger<T> _logger;
        protected readonly ICorrelationIdGenerator _correlationIdGenerator;
        protected readonly IConfiguration _configuration;
        protected readonly IAmazonDynamoDB _dynamoDb;

        public BaseLogger(ILogger<T> logger, ICorrelationIdGenerator correlationIdGenerator, IConfiguration configuration,
            IAmazonDynamoDB dynamoDb)
        {
            _logger = logger;
            _correlationIdGenerator = correlationIdGenerator;
            _configuration = configuration;
            _dynamoDb = dynamoDb;
        }

        private string ResolveCorrelationId(string? passedCorrelationId)
        {
            if (!string.IsNullOrWhiteSpace(passedCorrelationId))
            {
                _correlationIdGenerator.Set(passedCorrelationId);
                return passedCorrelationId;
            }

            var currentId = _correlationIdGenerator.Get();

            if (string.IsNullOrWhiteSpace(currentId))
            {
                currentId = Guid.NewGuid().ToString();
                _correlationIdGenerator.Set(currentId);
            }

            return currentId;
        }


        public virtual void LogInformation(string message, BaseLogType type, object? data = null, string? correlationId = null)
        {
            if (_configuration["CustomLogging:LogInfo"] == "True")
            {
                var activeId = ResolveCorrelationId(correlationId);
                _logger.LogInformation("[CorrelationId: {CorrelationId}] [Type: {Type}] {Message}", activeId, type, message);
                SendToDynamoDb("Information", type, message, data, activeId);
            }
        }

        public virtual void LogError(string message, BaseLogType type, object? data = null, string? correlationId = null)
        {
            if (_configuration["CustomLogging:LogError"] == "True")
            {
                var activeId = ResolveCorrelationId(correlationId);
                _logger.LogError("[CorrelationId: {CorrelationId}] {Message}", activeId, message);
                SendToDynamoDb("Error", type, message, data, activeId);
            }
        }

        public virtual void LogWarning(string message, BaseLogType type, object? data = null, string? correlationId = null)
        {
            if (_configuration["CustomLogging:LogWarning"] == "True")
            {
                var activeId = ResolveCorrelationId(correlationId);
                _logger.LogWarning("[CorrelationId: {CorrelationId}] {Message}", activeId, message);
                SendToDynamoDb("Warning", type, message, data, activeId);
            }
        }

        private void SendToDynamoDb(string logLevel, BaseLogType type, string message, object? data, string correlationId)
        {
            if (type != BaseLogType.EVENT && _configuration["CustomLogging:SendLogToDB"] != "True") return;

            try
            {
                var tabela = Table.LoadTable(_dynamoDb, "cs-app-log");

                var logDoc = new Document();
                logDoc["CorrelationId"] = correlationId;
                logDoc["Timestamp"] = DateTime.UtcNow.ToString("o");
                logDoc["Type"] = (int)type;
                logDoc["LogLevel"] = logLevel;
                logDoc["Caller"] = typeof(T).FullName ?? "Unknown";
                logDoc["Message"] = message;

                if (data != null)
                {
                    if (data is Exception ex)
                    {
                        logDoc["ExceptionType"] = ex.GetType().FullName;
                        logDoc["StackTrace"] = ex.StackTrace ?? "N/A";
                        logDoc["Data"] = ex.Message; 
                    }
                    else
                    {
                        var options = new JsonSerializerOptions
                        {
                           
                            ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
                            WriteIndented = false
                        };

                        logDoc["Data"] = JsonSerializer.Serialize(data, options);
                    }
                }

              
                Task.Run(async () =>
                {
                    try
                    {
                        await tabela.PutItemAsync(logDoc);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogCritical(ex, "Erro em background ao persistir registro no DynamoDB.");
                    }
                });
            }
            catch (Exception ex)
            {
                
                _logger.LogCritical(ex, "Falha crítica na preparação do log para o DynamoDB.");
            }
        }
    }
}