namespace Shared.Kernel.Common;

// Row is the row of the Excel sheet, or the position (1, 2, 3...) of the item in a JSON file
public sealed record ImportRowResult(
    int Row,
    bool Success,
    Guid? Id,
    string? Key,
    IReadOnlyList<string> Errors
);

// With DryRun nothing is saved and Succeeded counts the rows that would have been saved
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
