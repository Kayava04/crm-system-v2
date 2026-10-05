namespace Enrollments.Contracts;

public interface IEnrollmentLifecycle
{
    Task<StudentEnrollmentsChangeResult> SuspendForStudentAsync(Guid studentId, CancellationToken ct = default);

    Task<StudentEnrollmentsChangeResult> RestoreForStudentAsync(Guid studentId, CancellationToken ct = default);
}

public sealed record StudentEnrollmentsChangeResult(
    int EnrollmentsCount,
    int CancelledLessons,
    int RestoredLessons,
    int LessonsLeftToSchedule
);
