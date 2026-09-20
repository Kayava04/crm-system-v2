using Scheduling.Domain.Entities;
using Scheduling.Domain.Enums;
using Shared.Kernel.Abstractions;

namespace Scheduling.Application.Abstractions;

public interface IScheduleRepository : IRepository<Schedule>
{
    Task<bool> HasTeacherConflictAsync(
        Guid teacherId,
        DateTime start,
        int durationMinutes,
        Guid? excludeScheduleId = null,
        CancellationToken ct = default
    );

    Task AddRangeAsync(IReadOnlyCollection<Schedule> schedules, CancellationToken ct = default);

    Task<IReadOnlyList<Schedule>> GetOpenByTeacherInRangeAsync(
        Guid teacherId,
        DateTime from,
        DateTime to,
        CancellationToken ct = default
    );

    // Lessons that are held or still to be held (everything except Cancelled)
    Task<int> CountActiveByTargetAsync(
        Guid? enrollmentId,
        Guid? groupId,
        CancellationToken ct = default
    );

    Task<IReadOnlyList<Schedule>> GetFutureOpenAsync(
        Guid? enrollmentId,
        Guid? groupId,
        DateTime from,
        CancellationToken ct = default
    );

    Task<IReadOnlyList<Schedule>> GetFutureOpenByTeacherAsync(
        Guid teacherId,
        DateTime from,
        CancellationToken ct = default
    );

    // Upcoming lessons cancelled by the system for the given reason; filter by enrollment or by teacher
    Task<IReadOnlyList<Schedule>> GetFutureCancelledAsync(
        Guid? enrollmentId,
        Guid? teacherId,
        CancellationReason reason,
        DateTime from,
        CancellationToken ct = default
    );

    Task<bool> HasTeacherHistoryAsync(Guid teacherId, CancellationToken ct = default);

    Task<IReadOnlyList<Schedule>> GetForCalendarAsync(
        Guid? teacherId,
        IReadOnlyCollection<Guid> enrollmentIds,
        IReadOnlyCollection<Guid> groupIds,
        DateTime from,
        DateTime to,
        CancellationToken ct = default
    );

    Task<(IReadOnlyList<Schedule> Schedules, int TotalCount)> GetAllAsync(
        Guid? enrollmentId,
        Guid? groupId,
        Guid? teacherId,
        ScheduleStatus? status,
        DateTime? dateFrom,
        DateTime? dateTo,
        int page,
        int pageSize,
        CancellationToken ct = default
    );

    Task<int> CountCompletedByTeacherAsync(
        Guid teacherId,
        DateTime from,
        DateTime to,
        CancellationToken ct = default
    );

    Task<IReadOnlyList<Guid>> GetEnrollmentIdsByTeacherAsync(
        Guid teacherId,
        CancellationToken ct = default
    );
}
