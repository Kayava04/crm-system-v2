namespace Shared.Kernel.Common;

public sealed record ImportRowResult(
    int Row,
    bool Success,
    Guid? Id,
    string? Key,
    IReadOnlyList<string> Errors
);

public sealed record ImportResponse(
    string FileType,
    string Language,
    bool DryRun,
    bool AllOrNothing,
    int Total,
    int Succeeded,
    int Failed,
    IReadOnlyList<string> IgnoredColumns,
    IReadOnlyList<ImportRowResult> Rows
);
