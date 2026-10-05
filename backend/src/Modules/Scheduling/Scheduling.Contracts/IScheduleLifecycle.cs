namespace Scheduling.Contracts;

public interface IScheduleLifecycle
{
    Task<int> CancelForEnrollmentAsync(Guid enrollmentId, CancellationToken ct = default);

    Task<EnrollmentLessonsRestoreResult> RestoreForEnrollmentAsync(Guid enrollmentId, CancellationToken ct = default);

    Task<int> CancelForTeacherAsync(Guid teacherId, CancellationToken ct = default);

    Task<TeacherLessonsRestoreResult> RestoreForTeacherAsync(Guid teacherId, CancellationToken ct = default);
}

public sealed record EnrollmentLessonsRestoreResult(int RestoredCount, int SkippedCount, int LessonsLeftToSchedule);

public sealed record TeacherLessonsRestoreResult(int RestoredCount, int SkippedCount);
