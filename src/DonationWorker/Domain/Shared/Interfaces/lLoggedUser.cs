using DonationWorker.Domain.Shared.Primitives;

public interface IUserContext
{
    SystemUser? GetUser();
}