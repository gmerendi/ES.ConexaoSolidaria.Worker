using DonationWorker.Domain.Entities.Campanhas;
using DonationWorker.Domain.Entities.Doacoes;
using DonationWorker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DonationWorker.Infrastructure.Repositories;

public sealed class DoacaoRepository : EFRepository<Doacao>, IDoacaoRepository
{
    private readonly string _connectionString;

    public DoacaoRepository(ApplicationDbContext context, IConfiguration configuration) : base(context)
    {
        _connectionString = configuration.GetConnectionString("ConnectionString") ?? "";
    }



    public async Task<Doacao?> ObterPorCorrelationIdAsync(string correlationId, CancellationToken ct = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(u => u.CorrelationId.ToLower() == correlationId.ToLower(), ct);
    }
}
