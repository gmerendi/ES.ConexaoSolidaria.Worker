using DonationWorker.Domain.Shared.Interfaces;
using DonationWorker.Infrastructure.Services.Security;

namespace DonationWorker.Infrastructure.Extensions
{
    public static class AuthenticationExtensions
    {
        public static IServiceCollection AddAuthenticationServices(this IServiceCollection services, IConfiguration configuration, ILogger logger)
        {
            services.AddScoped<ICryptoService, CryptoService>();
            logger.LogInformation(" ***** CryptoService inicializado.");

            services.AddScoped<ITokenService, TokenService>();
            logger.LogInformation(" ***** TokenService inicializado.");

            return services;
        }
    }
}
