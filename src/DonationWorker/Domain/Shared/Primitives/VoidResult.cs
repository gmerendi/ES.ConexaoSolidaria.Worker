namespace DonationWorker.Domain.Shared.Primitives;

public class VoidResult
{
    public bool IsSuccess { get; }
    public string? Error { get; }

    private VoidResult(bool isSuccess, string? error)
    {
        IsSuccess = isSuccess;
        Error     = error;
    }

    public static VoidResult Success()             => new(true, null);
    public static VoidResult Failure(string error) => new(false, error);
}
