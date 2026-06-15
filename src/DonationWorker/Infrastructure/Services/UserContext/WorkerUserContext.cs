// Infrastructure/Services/UserContext/WorkerUserContext.cs
using DonationWorker.Domain.Enums;
using DonationWorker.Domain.Shared.Primitives;

namespace DonationWorker.Infrastructure.Services.UserContext;

public class WorkerUserContext : IUserContext
{
    public SystemUser? GetUser() => new SystemUser(
        Guid.Empty,
        "Donation Worker",
        "00000000000",
        "worker@conexaosolidaria.internal",
        Perfil.GESTOR_ONG.ToString(),
        EntityStatus.ACTIVE.ToString());
}