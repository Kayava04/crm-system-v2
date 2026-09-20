using Microsoft.EntityFrameworkCore;
using Scheduling.Application.Abstractions;
using Scheduling.Domain.Entities;
using Scheduling.Domain.Enums;
using Scheduling.Infrastructure.Postgres.Persistence;

namespace Scheduling.Infrastructure.Postgres.Repositories;

internal sealed class ScheduleRepository(SchedulingDbContext context) : IScheduleRepository
{
    // Upper bound for a lesson length (see validators); used to narrow the conflict search window
    private const int MaxDurationMinutes = 480;

    public async Task<IReadOnlyList<Schedule>> GetAllAsync(CancellationToken ct = default) =>
        await context.Schedules
            .AsNoTracking()
            .OrderByDescending(s => s.ScheduledDate)
            .ToListAsync(ct);

    public async Task<Schedule?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await context.Schedules
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task AddAsync(Schedule entity, CancellationToken ct = default) =>
        await context.Schedules.AddAsync(entity, ct);

    public Task UpdateAsync(Schedule entity, CancellationToken ct = default)
    {
        context.Schedules.Update(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Schedule entity, CancellationToken ct = default)
    {
        context.Schedules.Remove(entity);
        return Task.CompletedTask;
    }

    public async Task<bool> HasTeacherConflictAsync(
        Guid teacherId,
        DateTime start,
        int durationMinutes,
        Guid? excludeScheduleId = null,
        CancellationToken ct = default)
    {
        var end = start.AddMinutes(durationMinutes);
        var windowStart = start.AddMinutes(-MaxDurationMinutes);

        var candidates = await context.Schedules
            .AsNoTracking()
            .Where(s => s.TeacherId == teacherId
                && (s.Status == ScheduleStatus.Scheduled || s.Status == ScheduleStatus.Rescheduled)
                && s.ScheduledDate > windowStart
                && s.ScheduledDate < end
                && (excludeScheduleId == null || s.Id != excludeScheduleId))
            .ToListAsync(ct);

        return candidates.Any(s => s.EndDate > start);
    }

    public async Task<(IReadOnlyList<Schedule> Schedules, int TotalCount)> GetAllAsync(
        Guid? enrollmentId,
        Guid? teacherId,
        ScheduleStatus? status,
        DateTime? dateFrom,
        DateTime? dateTo,
        int page,
        int pageSize,
        CancellationToken ct = default
    )
    {
        var query = context.Schedules
            .AsNoTracking()
            .AsQueryable();

        if (enrollmentId.HasValue)
            query = query.Where(s => s.EnrollmentId == enrollmentId.Value);

        if (teacherId.HasValue)
            query = query.Where(s => s.TeacherId == teacherId.Value);

        if (status.HasValue)
            query = query.Where(s => s.Status == status.Value);

        if (dateFrom.HasValue)
            query = query.Where(s => s.ScheduledDate >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(s => s.ScheduledDate <= dateTo.Value);

        var totalCount = await query.CountAsync(ct);

        var schedules = await query
            .OrderBy(s => s.ScheduledDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (schedules, totalCount);
    }

    public async Task<int> CountCompletedByTeacherAsync(
        Guid teacherId,
        DateTime from,
        DateTime to,
        CancellationToken ct = default) =>
        await context.Schedules
            .AsNoTracking()
            .CountAsync(s => s.TeacherId == teacherId
                && s.Status == ScheduleStatus.Completed
                && s.ScheduledDate >= from
                && s.ScheduledDate < to, ct);

    public async Task<IReadOnlyList<Guid>> GetEnrollmentIdsByTeacherAsync(
        Guid teacherId,
        CancellationToken ct = default) =>
        await context.Schedules
            .AsNoTracking()
            .Where(s => s.TeacherId == teacherId)
            .Select(s => s.EnrollmentId)
            .Distinct()
            .ToListAsync(ct);
}
