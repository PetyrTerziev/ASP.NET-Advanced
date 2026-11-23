namespace GuardPost.Web.ViewModels.Common;

/// <summary>Outcome of a business operation that can fail for a reason the user should see.</summary>
public class OperationResult
{
    private OperationResult(bool succeeded, string? error)
    {
        Succeeded = succeeded;
        Error = error;
    }

    public bool Succeeded { get; }

    public string? Error { get; }

    public static OperationResult Success() => new(true, null);

    public static OperationResult Failure(string error) => new(false, error);
}
