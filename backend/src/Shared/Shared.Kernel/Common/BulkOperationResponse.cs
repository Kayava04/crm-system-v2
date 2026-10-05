namespace Shared.Kernel.Common;

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
