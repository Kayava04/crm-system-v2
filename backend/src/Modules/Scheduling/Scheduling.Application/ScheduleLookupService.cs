using Scheduling.Application.Abstractions;
using Scheduling.Application.Services;
using Scheduling.Contracts;

namespace Scheduling.Application;

internal sealed class ScheduleLookupService(
    IScheduleRepository repository,
    IStudyGroupRepository groupRepository,
    ISchoolClock clock
) : IScheduleLookup
{
    public async Task<int> GetCompletedLessonsCountAsync(
        Guid teacherId,
        DateTime from,
        DateTime to,
        CancellationToken ct = default) =>
        await repository.CountCompletedByTeacherAsync(teacherId, from, to, ct);

    public async Task<UpcomingLessonsResult> GetUpcomingLessonsAsync(
        DateTime from,
        DateTime to,
        CancellationToken ct = default)
    {
        var lessons = await repository.GetOpenInRangeAsync(from, to, ct);

        var groups = (await groupRepository.GetByIdsAsync(
                lessons.Where(l => l.GroupId.HasValue).Select(l => l.GroupId!.Value).Distinct().ToList(), ct))
            .ToDictionary(g => g.Id);

        var result = lessons.Select(l =>
        {
            groups.TryGetValue(l.GroupId ?? Guid.Empty, out var group);

            IReadOnlyList<Guid> enrollmentIds = l.EnrollmentId is { } enrollmentId
                ? [enrollmentId]
                : group?.Members.Select(m => m.EnrollmentId).ToList() ?? [];

            return new UpcomingLesson(l.Id, l.ScheduledDate, l.DurationMinutes, l.TeacherId, group?.Name, enrollmentIds);
        }).ToList();

        return new UpcomingLessonsResult(clock.TimeZoneId, result);
    }

    public async Task<bool> HasTeacherHistoryAsync(Guid teacherId, CancellationToken ct = default) =>
        await repository.HasTeacherHistoryAsync(teacherId, ct);

    public async Task<IReadOnlyList<Guid>> GetEnrollmentIdsByTeacherAsync(
        Guid teacherId,
        CancellationToken ct = default) =>
        await repository.GetEnrollmentIdsByTeacherAsync(teacherId, ct);
}
