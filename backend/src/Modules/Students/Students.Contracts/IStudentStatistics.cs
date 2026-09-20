namespace Students.Contracts;

public interface IStudentStatistics
{
    Task<StudentSummaryResult> GetSummaryAsync(CancellationToken ct = default);
}

public sealed record StudentSummaryResult(
    int Total,
    IReadOnlyDictionary<string, int> ByStatus
);
