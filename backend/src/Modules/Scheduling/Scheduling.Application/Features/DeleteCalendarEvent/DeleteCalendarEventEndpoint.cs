using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Scheduling.Application.Abstractions;
using Scheduling.Application.Services;
using Shared.Kernel.Abstractions;

namespace Scheduling.Application.Features.DeleteCalendarEvent;

public static class DeleteCalendarEventEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapDelete("/events/{id:guid}", Handle)
             .RequireAuthorization()
             .WithName("DeleteCalendarEvent")
             .WithSummary("Delete a calendar event: its owner for a personal one, a schedule manager for one visible to everyone")
             .Produces(StatusCodes.Status204NoContent)
             .ProducesProblem(StatusCodes.Status401Unauthorized)
             .ProducesProblem(StatusCodes.Status403Forbidden)
             .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        Guid id,
        ClaimsPrincipal principal,
        ICalendarEventRepository repository,
        ISchedulingUnitOfWork unitOfWork,
        ILogger<Guid> logger,
        CancellationToken ct
    )
    {
        var claim = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        if (!Guid.TryParse(claim, out var userId))
            return Results.Problem(detail: "Invalid user identity.", statusCode: StatusCodes.Status401Unauthorized);

        var calendarEvent = await repository.GetByIdAsync(id, ct);
        if (calendarEvent is null)
            return Results.Problem(detail: $"Calendar event with id '{id}' not found.", statusCode: StatusCodes.Status404NotFound);

        var forbidden = CalendarEventAccess.CheckCanManage(calendarEvent, principal, userId);
        if (forbidden is not null)
            return forbidden;

        await repository.DeleteAsync(calendarEvent, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Calendar event {CalendarEventId} deleted by {UserId}", id, userId);

        return Results.NoContent();
    }
}
