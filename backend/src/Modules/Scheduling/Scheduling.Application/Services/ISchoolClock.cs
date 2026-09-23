namespace Scheduling.Application.Services;

// Lessons are agreed in the school's local time but stored in UTC
public interface ISchoolClock
{
    string TimeZoneId { get; }

    DateOnly Today { get; }

    DateTime ToUtc(DateOnly date, TimeOnly time);
}

internal sealed class SchoolClock(string timeZoneId) : ISchoolClock
{
    private readonly TimeZoneInfo _timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

    public string TimeZoneId => timeZoneId;

    public DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _timeZone));

    public DateTime ToUtc(DateOnly date, TimeOnly time)
    {
        var local = date.ToDateTime(time, DateTimeKind.Unspecified);

        // A time skipped by the spring DST switch does not exist; move it to the first valid moment
        if (_timeZone.IsInvalidTime(local))
            local = local.AddHours(1);

        return TimeZoneInfo.ConvertTimeToUtc(local, _timeZone);
    }
}
