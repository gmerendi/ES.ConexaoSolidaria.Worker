using DonationWorker.Domain.Entities.Campanhas;
using DonationWorker.Domain.Entities.Doacoes;
using DonationWorker.Infrastructure.Repositories;

namespace DonationWorker.Infrastructure.Extensions
{
    public static class RepositoryExtensions
    {
        public static IServiceCollection AddRepositories(this IServiceCollection services, ILogger logger)
        {
            services.AddScoped<ICampanhaRepository, CampanhaRepository>();
            services.AddScoped<IDoacaoRepository, DoacaoRepository>();
            logger.LogInformation(" ***** CampanhaRepository service inicializado.");
            return services;
        }
    }
}