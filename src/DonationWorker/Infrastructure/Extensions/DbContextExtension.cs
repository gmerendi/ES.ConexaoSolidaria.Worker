using DonationWorker.Domain.Shared.Interfaces;
using DonationWorker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DonationWorker.Infrastructure.Extensions
{
    public static class DbContextExtensions
    {
        public static IServiceCollection AddDbContext(this IServiceCollection services, IConfiguration configuration, ILogger logger)
        {
            var connectionString = configuration.GetConnectionString("Database");

            if (string.IsNullOrEmpty(connectionString))
                logger.LogWarning(" ***** ⚠️ - Connection String (Application) nula ou vazia");
            else
                logger.LogInformation(" ***** ✅ - Connection String (Application) encontrada");

            // DbContext Principal com Interceptor de Auditoria
            services.AddDbContext<ApplicationDbContext>((sp, options) =>
            {
                options.UseNpgsql(connectionString);
                options.AddInterceptors(sp.GetRequiredService<AuditInterceptor>());
            }, ServiceLifetime.Scoped);
            logger.LogInformation(" ***** DbContext service inicializado.");

            services.AddScoped<IUnitOfWork, UnitOfWork>();
            logger.LogInformation(" ***** Unit of Work service inicializado.");

            return services;
        }
    }
}