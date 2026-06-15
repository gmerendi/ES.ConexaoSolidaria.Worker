using DonationWorker.Domain.Shared.Interfaces;
using System.IdentityModel.Tokens.Jwt;

namespace DonationWorker.Infrastructure.Services.Security;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public TimeSpan GetTokenTimeToExpire(string token)
    {

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        var dataExpiracaoToken = jwtToken.ValidTo; // Em UTC
        var tempoRestante = dataExpiracaoToken - DateTime.UtcNow;

        return tempoRestante > TimeSpan.Zero ? tempoRestante : TimeSpan.Zero;
    }
}