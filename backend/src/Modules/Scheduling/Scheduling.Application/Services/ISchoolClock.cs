namespace Scheduling.Application.Services;

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

        if (_timeZone.IsInvalidTime(local))
            local = local.AddHours(1);

        return TimeZoneInfo.ConvertTimeToUtc(local, _timeZone);
    }
}
