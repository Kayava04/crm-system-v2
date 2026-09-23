using System.Security.Claims;
using FluentValidation;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Scheduling.Application.Abstractions;
using Scheduling.Application.Features.GetCalendarEvents;
using Scheduling.Domain.Entities;
using Scheduling.Domain.Enums;
using Shared.Kernel.Abstractions;

namespace Scheduling.Application.Features.CreateCalendarEvent;

public sealed record CreateCalendarEventRequest(
    CalendarEventVisibility Visibility,
    string Title,
    string? Description,
    DateTime StartsAt,
    DateTime EndsAt,
    bool IsAllDay
);

public sealed class CreateCalendarEventValidator : AbstractValidator<CreateCalendarEventRequest>
{
    public CreateCalendarEventValidator()
    {
        RuleFor(x => x.Visibility)
            .IsInEnum().WithMessage("Invalid visibility.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.")
            .When(x => x.Description is not null);

        RuleFor(x => x.StartsAt)
            .NotEmpty().WithMessage("Start time is required.");

        RuleFor(x => x.EndsAt)
            .NotEmpty().WithMessage("End time is required.")
            .GreaterThanOrEqualTo(x => x.StartsAt).WithMessage("An event cannot end before it starts.");
    }
}

public static class CreateCalendarEventEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/events", Handle)
             .RequireAuthorization()
             .WithName("CreateCalendarEvent")
             .WithSummary("Create a personal reminder, or (with CanManageSchedule) a school-wide notice")
             .Produces<CalendarEventResponse>(StatusCodes.Status201Created)
             .ProducesValidationProblem()
             .ProducesProblem(StatusCodes.Status401Unauthorized)
             .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> Handle(
        CreateCalendarEventRequest request,
        ClaimsPrincipal principal,
        IValidator<CreateCalendarEventRequest> validator,
        ICalendarEventRepository repository,
        ISchedulingUnitOfWork unitOfWork,
        ILogger<CreateCalendarEventRequest> logger,
        CancellationToken ct
    )
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
            return Results.ValidationProblem(validationResult.ToDictionary());

        var claim = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        if (!Guid.TryParse(claim, out var userId))
            return Results.Problem(detail: "Invalid user identity.", statusCode: StatusCodes.Status401Unauthorized);

        // Anyone may keep a personal reminder; only schedule managers may post something everyone sees
        if (request.Visibility == CalendarEventVisibility.Everyone
            && !principal.HasClaim("permission", nameof(SystemPermission.CanManageSchedule)))
            return Results.Problem(
                detail: "Only a schedule manager can create an event visible to everyone.",
                statusCode: StatusCodes.Status403Forbidden);

        var calendarEvent = CalendarEvent.Create(
            userId, request.Visibility, request.Title, request.Description, request.StartsAt, request.EndsAt, request.IsAllDay);

        await repository.AddAsync(calendarEvent, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Calendar event {CalendarEventId} created by {UserId} ({Visibility})", calendarEvent.Id, userId, request.Visibility);

        var response = new CalendarEventResponse(
            calendarEvent.Id, calendarEvent.Visibility, calendarEvent.Title, calendarEvent.Description,
            calendarEvent.StartsAt, calendarEvent.EndsAt, calendarEvent.IsAllDay, IsMine: true);

        return Results.Created($"/api/calendar/events/{calendarEvent.Id}", response);
    }
}
