using Scheduling.Domain.Entities;
using Shared.Kernel.Abstractions;

namespace Scheduling.Application.Abstractions;

public interface ICalendarEventRepository : IRepository<CalendarEvent>
{
    Task<IReadOnlyList<CalendarEvent>> GetVisibleInRangeAsync(
        Guid userId,
        DateTime from,
        DateTime to,
        CancellationToken ct = default
    );
}
