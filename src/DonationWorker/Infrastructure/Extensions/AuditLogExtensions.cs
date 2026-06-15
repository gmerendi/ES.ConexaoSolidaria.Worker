using Amazon.DynamoDBv2;
using DonationWorker.Infrastructure.Services.AuditLog;
using DonationWorker.Infrastructure.Services.UserContext;

namespace DonationWorker.Infrastructure.Extensions
{
    public static class AuditLogExtensions
    {
        public static IServiceCollection AddAuditLog(this IServiceCollection services, IConfiguration configuration, ILogger logger)
        {
            

            // Audit Logs
            var dynamoDbConn = Environment.GetEnvironmentVariable("ConnectionStrings__AuditLog");
            var applicationType = Environment.GetEnvironmentVariable("Application__Type");
            if (applicationType == "LOCAL")
            {
                // LOCAL: Usa URL do container e chaves 'local'
                var credentials = new Amazon.Runtime.BasicAWSCredentials("local", "local");
                var config = new AmazonDynamoDBConfig { ServiceURL = dynamoDbConn };
                services.AddSingleton<IAmazonDynamoDB>(new AmazonDynamoDBClient(credentials, config));
            }
            else
            {
                // EKS/AWS: DEIXE O SDK GERENCIAR TUDO
                // Isso fará o SDK ler o Token injetado pela Service Account automaticamente
                services.AddAWSService<IAmazonDynamoDB>();
            }
            logger.LogInformation(" ***** DynamoDb inicializado.");


            services.AddScoped<IAuditLogService, AuditLogService>();
            services.AddScoped<IUserContext, WorkerUserContext>();
            services.AddScoped<AuditInterceptor>();
            logger.LogInformation(" ***** AuditLogService inicializado.");

            return services;
        }
    }
}
