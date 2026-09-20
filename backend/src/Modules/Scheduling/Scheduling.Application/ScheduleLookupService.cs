using Scheduling.Application.Abstractions;
using Scheduling.Contracts;

namespace Scheduling.Application;

internal sealed class ScheduleLookupService(IScheduleRepository repository) : IScheduleLookup
{
    public async Task<int> GetCompletedLessonsCountAsync(
        Guid teacherId,
        DateTime from,
        DateTime to,
        CancellationToken ct = default) =>
        await repository.CountCompletedByTeacherAsync(teacherId, from, to, ct);

    public async Task<bool> HasTeacherHistoryAsync(Guid teacherId, CancellationToken ct = default) =>
        await repository.HasTeacherHistoryAsync(teacherId, ct);

    public async Task<IReadOnlyList<Guid>> GetEnrollmentIdsByTeacherAsync(
        Guid teacherId,
        CancellationToken ct = default) =>
        await repository.GetEnrollmentIdsByTeacherAsync(teacherId, ct);
}
