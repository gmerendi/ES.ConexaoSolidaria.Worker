using DonationWorker.Domain.Shared.Interfaces;
using DonationWorker.Infrastructure.Services.Metrics;

namespace DonationWorker.Infrastructure.Extensions;

public static class MetricsExtension
{
    /// <summary>
    /// Adiciona IMetricsService ao container. Chamado em Program.cs junto às outras extensions.
    /// </summary>
    public static IServiceCollection AddMetricsServices(this IServiceCollection services, ILogger logger)
    {
        services.AddSingleton<IMetricsService, MetricsService>();
        logger.LogInformation(" ***** Metrics service (Prometheus) inicializado.");
        return services;
    }
}
