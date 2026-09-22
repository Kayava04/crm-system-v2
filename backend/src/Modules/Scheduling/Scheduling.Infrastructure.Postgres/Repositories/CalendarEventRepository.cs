using Microsoft.EntityFrameworkCore;
using Scheduling.Application.Abstractions;
using Scheduling.Domain.Entities;
using Scheduling.Domain.Enums;
using Scheduling.Infrastructure.Postgres.Persistence;

namespace Scheduling.Infrastructure.Postgres.Repositories;

internal sealed class CalendarEventRepository(SchedulingDbContext context) : ICalendarEventRepository
{
    public async Task<IReadOnlyList<CalendarEvent>> GetAllAsync(CancellationToken ct = default) =>
        await context.CalendarEvents
            .AsNoTracking()
            .OrderBy(e => e.StartsAt)
            .ToListAsync(ct);

    public async Task<CalendarEvent?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await context.CalendarEvents
            .FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task AddAsync(CalendarEvent entity, CancellationToken ct = default) =>
        await context.CalendarEvents.AddAsync(entity, ct);

    public Task UpdateAsync(CalendarEvent entity, CancellationToken ct = default)
    {
        context.CalendarEvents.Update(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(CalendarEvent entity, CancellationToken ct = default)
    {
        context.CalendarEvents.Remove(entity);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<CalendarEvent>> GetVisibleInRangeAsync(
        Guid userId,
        DateTime from,
        DateTime to,
        CancellationToken ct = default) =>
        await context.CalendarEvents
            .AsNoTracking()
            .Where(e => e.Visibility == CalendarEventVisibility.Everyone || e.CreatedByUserId == userId)
            .Where(e => e.StartsAt < to && e.EndsAt >= from)
            .OrderBy(e => e.StartsAt)
            .ToListAsync(ct);
}
