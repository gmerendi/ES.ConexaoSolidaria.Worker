namespace CS.Domain.Events
{
    public record UserCreatedEvent(Guid guidUsuario, string nomeCompleto, string email, string cpf, string? correlationId);
    public record DonationProcessedEvent(Guid guidUser, string nome, string email, Guid guidCampanha, string tituloCampanha, decimal valor, string status, string? correlationId);
    public record DonationCreatedEvent(Guid guidUser, string nome, string email, Guid guidCampanha, string tituloCampanha, string cpf, decimal valor, string status, string? correlationId);
}
