namespace Scheduling.Domain.Enums;

public enum CalendarEventVisibility
{
    // Only its owner ever sees it: a personal reminder
    Personal,

    // Every authenticated user sees it: a school-wide notice, holiday or meeting
    Everyone
}
