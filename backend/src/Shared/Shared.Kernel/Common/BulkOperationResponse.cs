namespace Shared.Kernel.Common;

// One entry per item of a bulk request, in the same order; Key is a human-friendly identifier (e.g. the email)
public sealed record BulkItemResult(
    int Index,
    bool Success,
    Guid? Id,
    string? Key,
    IReadOnlyList<string> Errors
);

public sealed record BulkOperationResponse(
    int Total,
    int Succeeded,
    int Failed,
    IReadOnlyList<BulkItemResult> Results
)
{
    public static BulkOperationResponse From(IReadOnlyList<BulkItemResult> results) =>
        new(results.Count, results.Count(r => r.Success), results.Count(r => !r.Success), results);
}
