using Scheduling.Application.Services;

namespace Crm.UnitTests.Services;

public class SchoolClockTests
{
    private readonly SchoolClock _clock = new("Europe/Kyiv");

    [Fact]
    public void Winter_time_is_utc_plus_two()
    {
        var utc = _clock.ToUtc(new DateOnly(2030, 1, 15), new TimeOnly(18, 0));

        Assert.Equal(new DateTime(2030, 1, 15, 16, 0, 0, DateTimeKind.Utc), utc);
    }

    [Fact]
    public void Summer_time_is_utc_plus_three()
    {
        var utc = _clock.ToUtc(new DateOnly(2030, 7, 15), new TimeOnly(18, 0));

        Assert.Equal(new DateTime(2030, 7, 15, 15, 0, 0, DateTimeKind.Utc), utc);
    }

    [Fact]
    public void The_same_local_time_changes_utc_hour_across_the_autumn_switch()
    {
        // Kyiv leaves summer time on the last Sunday of October
        var before = _clock.ToUtc(new DateOnly(2030, 10, 22), new TimeOnly(18, 0));
        var after = _clock.ToUtc(new DateOnly(2030, 10, 29), new TimeOnly(18, 0));

        Assert.Equal(15, before.Hour);
        Assert.Equal(16, after.Hour);
    }

    [Fact]
    public void A_time_skipped_by_the_spring_switch_moves_to_the_first_valid_moment()
    {
        // 31 March 2030, 03:30 does not exist in Kyiv (clocks jump from 03:00 to 04:00)
        var utc = _clock.ToUtc(new DateOnly(2030, 3, 31), new TimeOnly(3, 30));

        Assert.Equal(new DateTime(2030, 3, 31, 1, 30, 0, DateTimeKind.Utc), utc);
    }

    [Fact]
    public void Utc_zone_needs_no_conversion()
    {
        var clock = new SchoolClock("UTC");

        Assert.Equal(new DateTime(2030, 1, 15, 18, 0, 0, DateTimeKind.Utc), clock.ToUtc(new DateOnly(2030, 1, 15), new TimeOnly(18, 0)));
        Assert.Equal("UTC", clock.TimeZoneId);
    }

    [Fact]
    public void Unknown_zone_fails_fast()
    {
        Assert.ThrowsAny<Exception>(() => new SchoolClock("Mars/Olympus"));
    }
}
