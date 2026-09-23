using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Scheduling.Application.Abstractions;
using Scheduling.Application.Services;
using Scheduling.Domain.Enums;

namespace Scheduling.Application.Features.GetCalendarEvents;

public sealed record CalendarEventResponse(
    Guid Id,
    CalendarEventVisibility Visibility,
    string Title,
    string? Description,
    DateTime StartsAt,
    DateTime EndsAt,
    bool IsAllDay,
    bool IsMine
);

public static class GetCalendarEventsEndpoint
{
    private const int MaxRangeDays = 366;

    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/events", Handle)
             .RequireAuthorization()
             .WithName("GetCalendarEvents")
             .WithSummary("List the arbitrary calendar events visible to the current user (own ones plus everyone's), defaults to the next 30 days")
             .Produces<List<CalendarEventResponse>>(StatusCodes.Status200OK)
             .ProducesValidationProblem()
             .ProducesProblem(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> Handle(
        ClaimsPrincipal principal,
        ICalendarEventRepository repository,
        ISchoolClock clock,
        CancellationToken ct,
        DateOnly? from = null,
        DateOnly? to = null
    )
    {
        var fromDate = from ?? clock.Today;
        var toDate = to ?? fromDate.AddDays(30);

        if (toDate < fromDate || toDate.DayNumber - fromDate.DayNumber > MaxRangeDays)
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["to"] = [$"Date range must be positive and not exceed {MaxRangeDays} days."]
            });

        var claim = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        if (!Guid.TryParse(claim, out var userId))
            return Results.Problem(detail: "Invalid user identity.", statusCode: StatusCodes.Status401Unauthorized);

        // Both bounds are inclusive days in school time, same convention as the lesson calendar
        var fromUtc = clock.ToUtc(fromDate, TimeOnly.MinValue);
        var toUtc = clock.ToUtc(toDate.AddDays(1), TimeOnly.MinValue);

        var events = await repository.GetVisibleInRangeAsync(userId, fromUtc, toUtc, ct);

        return Results.Ok(events.Select(e => new CalendarEventResponse(
            e.Id, e.Visibility, e.Title, e.Description, e.StartsAt, e.EndsAt, e.IsAllDay, e.CreatedByUserId == userId)).ToList());
    }
}
