namespace DonationWorker.Infrastructure.Services.Extensions
{
    public static class DiagnosticExtensions
    {
        public static void LogEnvironmentVariables(this IHost host, ILogger logger)
        {
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            if (env == "Development")
            {
                logger.LogInformation(" ***** Environment Variables ");

                // Dica: Use um Dictionary para evitar repetir variáveis raw1, raw2...
                var variables = new[]
                {
                    //Rabbitm
                    "RabbitMq__Host", 
                    "RabbitMq__Username", 
                    "RabbitMq__Password",

                    // JWT
                    "Jwt__SecretKey", 
                    "Jwt__Issuer", 
                    "Jwt__Audience",
                    "Jwt__ExpirationHours",

                    // Connection Strings
                    "ConnectionStrings__Database",
                    "ConnectionStrings__AuditLog", 
                    "ConnectionStrings__Redis",

                    // Queues
                    "USER_CREATED_QUEUE",
                    "DONATION_INTENT_QUEUE",
                    "DONATION_PERFORMED_QUEUE",
                    "NEW_CAMPAIGN_QUEUE", 
                    
                    // Outros
                    "Application__Type",
            };

                foreach (var variable in variables)
                {
                    var value = Environment.GetEnvironmentVariable(variable);
                    logger.LogInformation("{Variable} RAW = [{Value}]", variable, value ?? "NULO/VAZIO");
                }

                logger.LogInformation("======================================================");
            }
        }
    }
}
