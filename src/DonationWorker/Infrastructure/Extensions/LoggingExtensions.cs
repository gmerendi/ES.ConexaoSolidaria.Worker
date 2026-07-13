using DonationWorker.Domain.Shared.Interfaces;
using DonationWorker.Infrastructure.Services.Logging;

namespace DonationWorker.Infrastructure.Extensions
{
    public static class LoggingExtensions
    {
        public static IServiceCollection AddCustomLogging(this IServiceCollection services, ILogger logger)
        {
            services.AddTransient<ICorrelationIdGenerator, CorrelationIdGenerator>();
            logger.LogInformation(" ***** CorrelationIdGenerator service inicializado.");

            services.AddTransient<IBaseLoggerDbWriter, BaseLoggerDbWriter>();
            logger.LogInformation(" ***** BaseLoggerDbWriter service inicializado.");

            services.AddTransient(typeof(IBaseLogger<>), typeof(BaseLogger<>));
            logger.LogInformation(" ***** BaseLogger service inicializado.");

            return services;
        }
    }
}
