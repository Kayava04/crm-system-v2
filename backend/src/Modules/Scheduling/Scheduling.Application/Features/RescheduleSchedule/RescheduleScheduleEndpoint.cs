using FluentValidation;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Scheduling.Application.Abstractions;

namespace Scheduling.Application.Features.RescheduleSchedule;

public sealed record RescheduleScheduleRequest(DateTime ScheduledDate);

public sealed class RescheduleScheduleValidator : AbstractValidator<RescheduleScheduleRequest>
{
    public RescheduleScheduleValidator()
    {
        RuleFor(x => x.ScheduledDate)
            .NotEmpty().WithMessage("Scheduled date is required.")
            .Must(d => d.ToUniversalTime() > DateTime.UtcNow)
            .WithMessage("Scheduled date must be in the future.");
    }
}

public static class RescheduleScheduleEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPut("/{id:guid}/reschedule", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanManageSchedule))
             .WithName("RescheduleSchedule")
             .WithSummary("Reschedule a schedule to a new date")
             .Produces(StatusCodes.Status204NoContent)
             .ProducesValidationProblem()
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> Handle(
        Guid id,
        RescheduleScheduleRequest request,
        IValidator<RescheduleScheduleRequest> validator,
        IScheduleRepository repository,
        ISchedulingUnitOfWork unitOfWork,
        ILogger<RescheduleScheduleRequest> logger,
        CancellationToken ct
    )
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
            return Results.ValidationProblem(validationResult.ToDictionary());

        var schedule = await repository.GetByIdAsync(id, ct);
        if (schedule is null)
            return Results.Problem(
                detail: $"Schedule with id '{id}' not found.",
                statusCode: StatusCodes.Status404NotFound
            );

        if (!schedule.IsOpen)
        {
            logger.LogWarning("Schedule {ScheduleId} is {Status} and cannot be rescheduled", id, schedule.Status);

            return Results.Problem(
                detail: $"Schedule is {schedule.Status.ToString().ToLowerInvariant()} and cannot be rescheduled.",
                statusCode: StatusCodes.Status409Conflict
            );
        }

        var newDate = request.ScheduledDate.ToUniversalTime();

        var hasConflict = await repository.HasTeacherConflictAsync(
            schedule.TeacherId, newDate, schedule.DurationMinutes, id, ct);

        if (hasConflict)
        {
            logger.LogWarning(
                "Teacher {TeacherId} already has a lesson at {ScheduledDate}",
                schedule.TeacherId, newDate
            );

            return Results.Problem(
                detail: "Teacher already has a lesson at this time.",
                statusCode: StatusCodes.Status409Conflict
            );
        }

        schedule.Reschedule(newDate);

        await repository.UpdateAsync(schedule, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Schedule rescheduled: {ScheduleId} to {ScheduledDate}", id, newDate);

        return Results.NoContent();
    }
}
