using Amazon.DynamoDBv2;
using DonationWorker.Domain.Shared.Interfaces;
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

            var accessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
            var secretKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");
            var sessionToken = Environment.GetEnvironmentVariable("AWS_SESSION_TOKEN");
            var region = Environment.GetEnvironmentVariable("AWS_REGION") ?? "us-east-1";
            var dynamoDbUrl = Environment.GetEnvironmentVariable("DYNAMODB_SERVICE_URL");


            if (applicationType == "LOCAL")
            {
                // LOCAL: Usa URL do container e chaves 'local'
                var credentials = new Amazon.Runtime.BasicAWSCredentials("local", "local");
                var config = new AmazonDynamoDBConfig { ServiceURL = dynamoDbConn };
                services.AddSingleton<IAmazonDynamoDB>(new AmazonDynamoDBClient(credentials, config));
            }
            else if (applicationType == "LAB")
            {
                var credentials = new Amazon.Runtime.SessionAWSCredentials(accessKey, secretKey, sessionToken);
                var config = new AmazonDynamoDBConfig { ServiceURL = dynamoDbUrl };
                services.AddSingleton<IAmazonDynamoDB>(new AmazonDynamoDBClient(credentials, config));
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
