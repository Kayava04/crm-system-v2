using Scheduling.Domain.Enums;
using Shared.Kernel.Primitives;

namespace Scheduling.Domain.Entities;

// A free-form calendar entry that is not a lesson: a personal reminder or a school-wide notice.
// Lessons (Schedule) stay a separate, structured entity — this one carries no enrollment, group or
// teacher of its own, just a time range anyone (Personal) or everyone (Everyone) can see.
public sealed class CalendarEvent : AuditableEntity
{
    public Guid CreatedByUserId { get; private set; }
    public CalendarEventVisibility Visibility { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public DateTime StartsAt { get; private set; }
    public DateTime EndsAt { get; private set; }
    public bool IsAllDay { get; private set; }

    private CalendarEvent() { }

    public static CalendarEvent Create(
        Guid createdByUserId,
        CalendarEventVisibility visibility,
        string title,
        string? description,
        DateTime startsAt,
        DateTime endsAt,
        bool isAllDay)
    {
        var (start, end) = NormalizeRange(startsAt, endsAt);

        return new CalendarEvent
        {
            Id = Guid.NewGuid(),
            CreatedByUserId = createdByUserId,
            Visibility = visibility,
            Title = title,
            Description = description,
            StartsAt = start,
            EndsAt = end,
            IsAllDay = isAllDay,
            CreatedAt = DateTime.UtcNow
        };
    }

    // The owner never changes: an event created for oneself cannot be handed to someone else
    public void Update(
        CalendarEventVisibility visibility,
        string title,
        string? description,
        DateTime startsAt,
        DateTime endsAt,
        bool isAllDay)
    {
        var (start, end) = NormalizeRange(startsAt, endsAt);

        Visibility = visibility;
        Title = title;
        Description = description;
        StartsAt = start;
        EndsAt = end;
        IsAllDay = isAllDay;
        UpdatedAt = DateTime.UtcNow;
    }

    private static (DateTime Start, DateTime End) NormalizeRange(DateTime startsAt, DateTime endsAt)
    {
        var start = NormalizeToUtc(startsAt);
        var end = NormalizeToUtc(endsAt);

        if (end < start)
            throw new ArgumentException("An event cannot end before it starts.", nameof(endsAt));

        return (start, end);
    }

    // Npgsql only accepts UTC values for timestamptz columns
    private static DateTime NormalizeToUtc(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
}
