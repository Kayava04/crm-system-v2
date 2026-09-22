using Scheduling.Domain.Entities;
using Shared.Kernel.Abstractions;

namespace Scheduling.Application.Abstractions;

public interface ICalendarEventRepository : IRepository<CalendarEvent>
{
    // Everything the given user is allowed to see, overlapping [from, to): their own events plus everyone's
    Task<IReadOnlyList<CalendarEvent>> GetVisibleInRangeAsync(
        Guid userId,
        DateTime from,
        DateTime to,
        CancellationToken ct = default
    );
}
