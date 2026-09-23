using Scheduling.Domain.Entities;
using Scheduling.Domain.Enums;

namespace Crm.UnitTests.Domain;

public class CalendarEventTests
{
    private static readonly DateTime Start = new(2030, 5, 6, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime End = new(2030, 5, 6, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_keeps_who_made_it_and_who_may_see_it()
    {
        var owner = Guid.NewGuid();

        var reminder = CalendarEvent.Create(owner, CalendarEventVisibility.Personal, "Dentist", null, Start, End, isAllDay: false);

        Assert.Equal(owner, reminder.CreatedByUserId);
        Assert.Equal(CalendarEventVisibility.Personal, reminder.Visibility);
        Assert.Equal("Dentist", reminder.Title);
        Assert.Equal(Start, reminder.StartsAt);
        Assert.Equal(End, reminder.EndsAt);
        Assert.False(reminder.IsAllDay);
    }

    [Fact]
    public void An_event_cannot_end_before_it_starts()
    {
        Assert.Throws<ArgumentException>(() =>
            CalendarEvent.Create(Guid.NewGuid(), CalendarEventVisibility.Personal, "Bad", null, End, Start, isAllDay: false));

        var existing = CalendarEvent.Create(Guid.NewGuid(), CalendarEventVisibility.Personal, "Ok", null, Start, End, isAllDay: false);
        Assert.Throws<ArgumentException>(() => existing.Update(CalendarEventVisibility.Personal, "Ok", null, End, Start, isAllDay: false));
    }

    [Fact]
    public void An_event_starting_and_ending_at_the_same_instant_is_allowed()
    {
        var point = CalendarEvent.Create(Guid.NewGuid(), CalendarEventVisibility.Personal, "Deadline", null, Start, Start, isAllDay: false);

        Assert.Equal(point.StartsAt, point.EndsAt);
    }

    [Fact]
    public void A_naive_or_local_time_is_normalized_to_utc_for_storage()
    {
        var unspecified = new DateTime(2030, 6, 1, 8, 0, 0, DateTimeKind.Unspecified);

        var stored = CalendarEvent.Create(Guid.NewGuid(), CalendarEventVisibility.Everyone, "Holiday", null, unspecified, unspecified, isAllDay: true);

        Assert.Equal(DateTimeKind.Utc, stored.StartsAt.Kind);
        Assert.Equal(DateTimeKind.Utc, stored.EndsAt.Kind);
    }

    [Fact]
    public void Update_replaces_the_visible_fields_but_never_the_owner()
    {
        var owner = Guid.NewGuid();
        var reminder = CalendarEvent.Create(owner, CalendarEventVisibility.Personal, "Old title", "old", Start, End, isAllDay: false);
        var newStart = Start.AddDays(1);
        var newEnd = End.AddDays(1);

        reminder.Update(CalendarEventVisibility.Everyone, "New title", "new", newStart, newEnd, isAllDay: true);

        Assert.Equal(owner, reminder.CreatedByUserId);
        Assert.Equal(CalendarEventVisibility.Everyone, reminder.Visibility);
        Assert.Equal("New title", reminder.Title);
        Assert.Equal("new", reminder.Description);
        Assert.Equal(newStart, reminder.StartsAt);
        Assert.Equal(newEnd, reminder.EndsAt);
        Assert.True(reminder.IsAllDay);
        Assert.NotNull(reminder.UpdatedAt);
    }
}
