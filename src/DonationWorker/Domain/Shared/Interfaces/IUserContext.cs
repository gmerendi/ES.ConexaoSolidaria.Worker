using DonationWorker.Domain.Shared.Primitives;

namespace DonationWorker.Domain.Shared.Interfaces;

public interface IUserContext
{
    SystemUser GetUser();
}