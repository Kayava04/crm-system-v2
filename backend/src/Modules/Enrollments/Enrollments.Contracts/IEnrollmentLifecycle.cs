namespace Enrollments.Contracts;

// Used when a student becomes inactive or returns: their enrollments follow, and so does the calendar
public interface IEnrollmentLifecycle
{
    // Pauses every active enrollment of the student and cancels its upcoming lessons
    Task<StudentEnrollmentsChangeResult> SuspendForStudentAsync(Guid studentId, CancellationToken ct = default);

    // Resumes the enrollments that were paused by SuspendForStudentAsync and brings their lessons back
    Task<StudentEnrollmentsChangeResult> RestoreForStudentAsync(Guid studentId, CancellationToken ct = default);
}

public sealed record StudentEnrollmentsChangeResult(
    int EnrollmentsCount,
    int CancelledLessons,
    int RestoredLessons,
    int LessonsLeftToSchedule
);
