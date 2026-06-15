namespace CS.Domain.Events
{
    public record DonationProcessedEvent(Guid guidUser, string nome, string email, Guid guidCampanha, string tituloCampanha, decimal valor, string? correlationId);
    public record DonationCreatedEvent(Guid guidUser, string nome, string email, Guid guidCampanha, string tituloCampanha, string cpf, decimal valor, string? correlationId);
}
