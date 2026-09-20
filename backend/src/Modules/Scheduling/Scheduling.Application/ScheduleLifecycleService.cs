using Courses.Contracts;
using Enrollments.Contracts;
using Scheduling.Application.Abstractions;
using Scheduling.Contracts;
using Scheduling.Domain.Entities;
using Scheduling.Domain.Enums;
using Teachers.Contracts;

namespace Scheduling.Application;

internal sealed class ScheduleLifecycleService(
    IScheduleRepository repository,
    ISchedulingUnitOfWork unitOfWork,
    IEnrollmentLookup enrollmentLookup,
    ICourseLookup courseLookup,
    ITeacherVerifier teacherVerifier
) : IScheduleLifecycle
{
    public async Task<int> CancelForEnrollmentAsync(Guid enrollmentId, CancellationToken ct = default)
    {
        var upcoming = await repository.GetFutureOpenAsync(enrollmentId, null, DateTime.UtcNow, ct);

        foreach (var lesson in upcoming)
            lesson.Cancel(CancellationReason.EnrollmentInactive);

        if (upcoming.Count > 0)
            await unitOfWork.SaveChangesAsync(ct);

        return upcoming.Count;
    }

    public async Task<EnrollmentLessonsRestoreResult> RestoreForEnrollmentAsync(
        Guid enrollmentId,
        CancellationToken ct = default)
    {
        var cancelled = await repository.GetFutureCancelledAsync(
            enrollmentId, null, CancellationReason.EnrollmentInactive, DateTime.UtcNow, ct);

        var teacherAvailable = new Dictionary<Guid, bool>();
        int restored = 0, skipped = 0;

        foreach (var lesson in cancelled)
        {
            if (!teacherAvailable.TryGetValue(lesson.TeacherId, out var available))
            {
                available = await teacherVerifier.IsAvailableAsync(lesson.TeacherId, ct);
                teacherAvailable[lesson.TeacherId] = available;
            }

            if (!available)
            {
                // The teacher is away as well: the lesson comes back together with the teacher
                lesson.Cancel(CancellationReason.TeacherUnavailable);
                skipped++;
                continue;
            }

            if (await repository.HasTeacherConflictAsync(
                    lesson.TeacherId, lesson.ScheduledDate, lesson.DurationMinutes, lesson.Id, ct))
            {
                skipped++;
                continue;
            }

            lesson.RestoreIfSystemCancelled();
            restored++;
        }

        if (cancelled.Count > 0)
            await unitOfWork.SaveChangesAsync(ct);

        return new EnrollmentLessonsRestoreResult(
            restored, skipped, await CountLessonsLeftToScheduleAsync(enrollmentId, ct));
    }

    public async Task<int> CancelForTeacherAsync(Guid teacherId, CancellationToken ct = default)
    {
        var upcoming = await repository.GetFutureOpenByTeacherAsync(teacherId, DateTime.UtcNow, ct);

        foreach (var lesson in upcoming)
            lesson.Cancel(CancellationReason.TeacherUnavailable);

        if (upcoming.Count > 0)
            await unitOfWork.SaveChangesAsync(ct);

        return upcoming.Count;
    }

    public async Task<TeacherLessonsRestoreResult> RestoreForTeacherAsync(Guid teacherId, CancellationToken ct = default)
    {
        var cancelled = await repository.GetFutureCancelledAsync(
            null, teacherId, CancellationReason.TeacherUnavailable, DateTime.UtcNow, ct);

        var enrollments = (await enrollmentLookup.GetByIdsAsync(
                cancelled.Where(l => l.EnrollmentId.HasValue).Select(l => l.EnrollmentId!.Value).Distinct().ToList(), ct))
            .ToDictionary(e => e.Id);

        int restored = 0, skipped = 0;

        foreach (var lesson in cancelled)
        {
            // An individual lesson of a suspended student stays cancelled until the student returns
            if (lesson.EnrollmentId is { } enrollmentId
                && (!enrollments.TryGetValue(enrollmentId, out var enrollment) || !enrollment.IsActive))
            {
                lesson.Cancel(CancellationReason.EnrollmentInactive);
                skipped++;
                continue;
            }

            if (await repository.HasTeacherConflictAsync(
                    lesson.TeacherId, lesson.ScheduledDate, lesson.DurationMinutes, lesson.Id, ct))
            {
                skipped++;
                continue;
            }

            lesson.RestoreIfSystemCancelled();
            restored++;
        }

        if (cancelled.Count > 0)
            await unitOfWork.SaveChangesAsync(ct);

        return new TeacherLessonsRestoreResult(restored, skipped);
    }

    private async Task<int> CountLessonsLeftToScheduleAsync(Guid enrollmentId, CancellationToken ct)
    {
        var enrollment = await enrollmentLookup.GetByIdAsync(enrollmentId, ct);
        if (enrollment is null)
            return 0;

        var course = await courseLookup.GetByIdAsync(enrollment.CourseId, ct);
        if (course is null || course.IsGroup)
            return 0;

        var scheduled = await repository.CountActiveByTargetAsync(enrollmentId, null, ct);

        return Math.Max(0, course.LessonsCount - scheduled);
    }
}
