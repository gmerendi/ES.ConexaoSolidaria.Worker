namespace DonationWorker.Domain.Shared.Interfaces
{
    public interface IMessageService
    {
        Task SendDonationProcessedEventMessage(Guid guidUser, string nome, string email, Guid guidCampanha, string tituloCampanha, decimal valor, string status, string correlationId, CancellationToken ct);
    }
}
