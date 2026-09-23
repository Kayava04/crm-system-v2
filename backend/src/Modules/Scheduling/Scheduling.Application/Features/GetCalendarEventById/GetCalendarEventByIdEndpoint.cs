using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Scheduling.Application.Abstractions;
using Scheduling.Application.Features.GetCalendarEvents;
using Scheduling.Domain.Enums;

namespace Scheduling.Application.Features.GetCalendarEventById;

public static class GetCalendarEventByIdEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/events/{id:guid}", Handle)
             .RequireAuthorization()
             .WithName("GetCalendarEventById")
             .WithSummary("Get one calendar event, if it is visible to the current user")
             .Produces<CalendarEventResponse>(StatusCodes.Status200OK)
             .ProducesProblem(StatusCodes.Status401Unauthorized)
             .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        Guid id,
        ClaimsPrincipal principal,
        ICalendarEventRepository repository,
        CancellationToken ct
    )
    {
        var claim = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        if (!Guid.TryParse(claim, out var userId))
            return Results.Problem(detail: "Invalid user identity.", statusCode: StatusCodes.Status401Unauthorized);

        var calendarEvent = await repository.GetByIdAsync(id, ct);

        // A personal event of someone else does not exist as far as the caller is concerned
        if (calendarEvent is null || (calendarEvent.Visibility == CalendarEventVisibility.Personal && calendarEvent.CreatedByUserId != userId))
            return Results.Problem(detail: $"Calendar event with id '{id}' not found.", statusCode: StatusCodes.Status404NotFound);

        return Results.Ok(new CalendarEventResponse(
            calendarEvent.Id, calendarEvent.Visibility, calendarEvent.Title, calendarEvent.Description,
            calendarEvent.StartsAt, calendarEvent.EndsAt, calendarEvent.IsAllDay, calendarEvent.CreatedByUserId == userId));
    }
}
