using DonationWorker.Domain.Shared.Interfaces;

namespace DonationWorker.Domain.Entities.Doacoes
{
    public interface IDoacaoRepository : IRepository<Doacao>
    {
        Task<Doacao?> ObterPorCorrelationIdAsync(string correlationId, CancellationToken ct = default);
    }
}
