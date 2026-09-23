using System.Security.Claims;
using FluentValidation;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Scheduling.Application.Abstractions;
using Scheduling.Application.Features.CreateCalendarEvent;
using Scheduling.Application.Features.GetCalendarEvents;
using Scheduling.Application.Services;
using Scheduling.Domain.Enums;
using Shared.Kernel.Abstractions;

namespace Scheduling.Application.Features.UpdateCalendarEvent;

public sealed record UpdateCalendarEventRequest(
    CalendarEventVisibility Visibility,
    string Title,
    string? Description,
    DateTime StartsAt,
    DateTime EndsAt,
    bool IsAllDay
);

public static class UpdateCalendarEventEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPut("/events/{id:guid}", Handle)
             .RequireAuthorization()
             .WithName("UpdateCalendarEvent")
             .WithSummary("Update a calendar event: its owner for a personal one, a schedule manager for one visible to everyone")
             .Produces<CalendarEventResponse>(StatusCodes.Status200OK)
             .ProducesValidationProblem()
             .ProducesProblem(StatusCodes.Status401Unauthorized)
             .ProducesProblem(StatusCodes.Status403Forbidden)
             .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        Guid id,
        UpdateCalendarEventRequest request,
        ClaimsPrincipal principal,
        IValidator<CreateCalendarEventRequest> validator,
        ICalendarEventRepository repository,
        ISchedulingUnitOfWork unitOfWork,
        ILogger<UpdateCalendarEventRequest> logger,
        CancellationToken ct
    )
    {
        // The request shape is identical to creation, so the same rules (title length, end >= start, ...) apply
        var validationResult = await validator.ValidateAsync(
            new CreateCalendarEventRequest(request.Visibility, request.Title, request.Description, request.StartsAt, request.EndsAt, request.IsAllDay), ct);
        if (!validationResult.IsValid)
            return Results.ValidationProblem(validationResult.ToDictionary());

        var claim = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        if (!Guid.TryParse(claim, out var userId))
            return Results.Problem(detail: "Invalid user identity.", statusCode: StatusCodes.Status401Unauthorized);

        var calendarEvent = await repository.GetByIdAsync(id, ct);
        if (calendarEvent is null)
            return Results.Problem(detail: $"Calendar event with id '{id}' not found.", statusCode: StatusCodes.Status404NotFound);

        var forbidden = CalendarEventAccess.CheckCanManage(calendarEvent, principal, userId);
        if (forbidden is not null)
            return forbidden;

        // Turning a personal reminder into something everyone sees still needs the same permission as creating one that way
        if (request.Visibility == CalendarEventVisibility.Everyone
            && !principal.HasClaim("permission", nameof(SystemPermission.CanManageSchedule)))
            return Results.Problem(
                detail: "Only a schedule manager can make an event visible to everyone.",
                statusCode: StatusCodes.Status403Forbidden);

        calendarEvent.Update(request.Visibility, request.Title, request.Description, request.StartsAt, request.EndsAt, request.IsAllDay);

        await repository.UpdateAsync(calendarEvent, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Calendar event {CalendarEventId} updated by {UserId}", calendarEvent.Id, userId);

        return Results.Ok(new CalendarEventResponse(
            calendarEvent.Id, calendarEvent.Visibility, calendarEvent.Title, calendarEvent.Description,
            calendarEvent.StartsAt, calendarEvent.EndsAt, calendarEvent.IsAllDay, calendarEvent.CreatedByUserId == userId));
    }
}
