using Enrollments.Application.Abstractions;
using Enrollments.Contracts;
using Microsoft.Extensions.Logging;
using Scheduling.Contracts;

namespace Enrollments.Application;

internal sealed class EnrollmentLifecycleService(
    IEnrollmentRepository repository,
    IEnrollmentUnitOfWork unitOfWork,
    IScheduleLifecycle scheduleLifecycle,
    ILogger<EnrollmentLifecycleService> logger
) : IEnrollmentLifecycle
{
    public async Task<StudentEnrollmentsChangeResult> SuspendForStudentAsync(
        Guid studentId,
        CancellationToken ct = default)
    {
        var enrollments = await repository.GetActiveByStudentAsync(studentId, ct);

        foreach (var enrollment in enrollments)
            enrollment.Suspend(bySystem: true);

        if (enrollments.Count > 0)
            await unitOfWork.SaveChangesAsync(ct);

        var cancelled = 0;

        foreach (var enrollment in enrollments)
            cancelled += await scheduleLifecycle.CancelForEnrollmentAsync(enrollment.Id, ct);

        logger.LogInformation(
            "Student {StudentId} became inactive: {Enrollments} enrollments suspended, {Lessons} lessons cancelled",
            studentId, enrollments.Count, cancelled);

        return new StudentEnrollmentsChangeResult(enrollments.Count, cancelled, 0, 0);
    }

    public async Task<StudentEnrollmentsChangeResult> RestoreForStudentAsync(
        Guid studentId,
        CancellationToken ct = default)
    {
        var enrollments = await repository.GetAutoSuspendedByStudentAsync(studentId, ct);

        foreach (var enrollment in enrollments)
            enrollment.Activate();

        if (enrollments.Count > 0)
            await unitOfWork.SaveChangesAsync(ct);

        int restored = 0, leftToSchedule = 0;

        foreach (var enrollment in enrollments)
        {
            var result = await scheduleLifecycle.RestoreForEnrollmentAsync(enrollment.Id, ct);

            restored += result.RestoredCount;
            leftToSchedule += result.LessonsLeftToSchedule;
        }

        logger.LogInformation(
            "Student {StudentId} returned: {Enrollments} enrollments resumed, {Lessons} lessons restored, {Left} left to schedule",
            studentId, enrollments.Count, restored, leftToSchedule);

        return new StudentEnrollmentsChangeResult(enrollments.Count, 0, restored, leftToSchedule);
    }
}
