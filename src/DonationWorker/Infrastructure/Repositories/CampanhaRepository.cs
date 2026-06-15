using DonationWorker.Domain.Entities.Campanhas;
using DonationWorker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DonationWorker.Infrastructure.Repositories;

public sealed class CampanhaRepository : EFRepository<Campanha>, ICampanhaRepository
{
    private readonly string _connectionString;

    public CampanhaRepository(ApplicationDbContext context, IConfiguration configuration) : base(context)
    {
        _connectionString = configuration.GetConnectionString("ConnectionString") ?? "";
    }




    public async Task<Campanha?> ObterPorTituloAsync(string titulo, CancellationToken ct = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(u => u.Titulo.Valor.ToLower() == titulo.ToLower(), ct);
    }


    public new async Task<List<Campanha>> ObterTodosAsync(int page = 1, int pageLength = 9999, CancellationToken cancellationToken = default)
    {

        return await _dbSet.Skip((page - 1) * pageLength)   // pula os itens das páginas anteriores
                     .Take(pageLength)                   // pega apenas o tamanho da página
                     .ToListAsync(cancellationToken);
    }


    public new async Task ObterEAlterarAsync(Campanha campanha, decimal doacao, CancellationToken cancellationToken = default)
    {

        await _dbSet.Where(c => c.Guid == campanha.Guid).ExecuteUpdateAsync(s =>
                            s.SetProperty(c => c.ValorArrecadado, c => c.ValorArrecadado + doacao));
    }
}
