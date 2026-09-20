namespace Scheduling.Contracts;

// Keeps the calendar in step with what happens to students, enrollments and teachers elsewhere in the system
public interface IScheduleLifecycle
{
    // Cancels upcoming individual lessons of an enrollment (it was suspended, completed or terminated)
    Task<int> CancelForEnrollmentAsync(Guid enrollmentId, CancellationToken ct = default);

    // Brings back the upcoming lessons cancelled by the system when the enrollment was deactivated
    Task<EnrollmentLessonsRestoreResult> RestoreForEnrollmentAsync(Guid enrollmentId, CancellationToken ct = default);

    // Cancels all upcoming lessons of a teacher who is no longer available (on leave, resigned, dismissed)
    Task<int> CancelForTeacherAsync(Guid teacherId, CancellationToken ct = default);

    // Brings back the lessons cancelled when the teacher became unavailable
    Task<TeacherLessonsRestoreResult> RestoreForTeacherAsync(Guid teacherId, CancellationToken ct = default);
}

// LessonsLeftToSchedule > 0 means the missed lessons must be scheduled again (POST /api/schedules/generate)
public sealed record EnrollmentLessonsRestoreResult(int RestoredCount, int SkippedCount, int LessonsLeftToSchedule);

public sealed record TeacherLessonsRestoreResult(int RestoredCount, int SkippedCount);
