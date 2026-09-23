namespace Enrollments.Contracts;

public interface IEnrollmentStatistics
{
    Task<EnrollmentSummaryResult> GetSummaryAsync(CancellationToken ct = default);
}

public sealed record EnrollmentSummaryResult(
    int Total,
    IReadOnlyDictionary<string, int> ByStatus,
    IReadOnlyList<EnrollmentCourseCount> ByCourse
);

public sealed record EnrollmentCourseCount(Guid CourseId, int Count);
