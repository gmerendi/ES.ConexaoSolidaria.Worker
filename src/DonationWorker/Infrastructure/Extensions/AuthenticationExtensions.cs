using DonationWorker.Domain.Shared.Interfaces;
using DonationWorker.Infrastructure.Services.Security;
using System.Text;

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

            //services.AddAuthentication(options =>
            //{
            //    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            //    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            //})
            //    .AddJwtBearer(options =>
            //    {
            //        options.TokenValidationParameters = new TokenValidationParameters
            //        {
            //            ValidateIssuerSigningKey = true, // 1. Valida a CHAVE de assinatura do token
            //            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:SecretKey"]!)),

            //            ValidateIssuer = true,           // 2. Valida o ISSUER (Quem emitiu o token)
            //            ValidIssuer = configuration["Jwt:Issuer"],

            //            ValidateAudience = true,         // 3. Valida o AUDIENCE (Quem pode usar o token)
            //            ValidAudience = configuration["Jwt:Audience"],

            //            ValidateLifetime = true,         // 4. Valida se o token EXPIROU
            //            ClockSkew = TimeSpan.Zero        // Remove a tolerância padrão de 5 minutos do .NET
            //        };
            //    });
            logger.LogInformation(" ***** JwtBearer inicializado.");

            return services;
        }
    }
}
