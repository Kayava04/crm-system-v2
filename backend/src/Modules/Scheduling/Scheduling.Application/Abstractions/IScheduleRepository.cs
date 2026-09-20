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

    Task<(IReadOnlyList<Schedule> Schedules, int TotalCount)> GetAllAsync(
        Guid? enrollmentId,
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
