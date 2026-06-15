using DonationWorker.Domain.Shared.Interfaces;

namespace DonationWorker.Domain.Entities.Campanhas
{
    public interface ICampanhaRepository : IRepository<Campanha>
    {
        Task<Campanha?> ObterPorTituloAsync(string email, CancellationToken ct = default);
        Task <List<Campanha>> ObterTodosAsync(int page, int pageLength, CancellationToken ct = default);
        Task ObterEAlterarAsync(Campanha campanha, decimal doacao, CancellationToken cancellationToken = default);
    }
}
