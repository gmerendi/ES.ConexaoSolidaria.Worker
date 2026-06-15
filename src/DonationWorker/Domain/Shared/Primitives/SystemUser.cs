namespace DonationWorker.Domain.Shared.Primitives
{
    public class SystemUser
    {
        public Guid Guid { get; init; }
        public string NomeCompleto { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string Cpf { get; init; } = string.Empty;
        public string Perfil { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;


        public SystemUser(Guid guid, string nomeCompleto, string cpf, string email, string perfil, string status)
        {
            Guid = guid;
            NomeCompleto = nomeCompleto;
            Cpf = cpf;
            Email = email;
            Perfil = perfil;
            Status = status;
        }
    }
}
