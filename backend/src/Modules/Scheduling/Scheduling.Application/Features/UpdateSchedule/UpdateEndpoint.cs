using FluentValidation;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Scheduling.Application.Abstractions;
using Teachers.Contracts;

namespace Scheduling.Application.Features.UpdateSchedule;

public sealed record UpdateScheduleRequest(
    Guid TeacherId,
    int DurationMinutes,
    string? Notes
);

public sealed class UpdateScheduleValidator : AbstractValidator<UpdateScheduleRequest>
{
    public UpdateScheduleValidator()
    {
        RuleFor(x => x.TeacherId)
            .NotEmpty().WithMessage("Teacher is required.");

        RuleFor(x => x.DurationMinutes)
            .InclusiveBetween(15, 480).WithMessage("Duration must be between 15 and 480 minutes.");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Notes must not exceed 500 characters.")
            .When(x => x.Notes is not null);
    }
}

public static class UpdateEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPut("/{id:guid}", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanManageSchedule))
             .WithName("UpdateSchedule")
             .WithSummary("Update a schedule (teacher, duration, notes)")
             .Produces(StatusCodes.Status204NoContent)
             .ProducesValidationProblem()
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> Handle(
        Guid id,
        UpdateScheduleRequest request,
        IValidator<UpdateScheduleRequest> validator,
        IScheduleRepository repository,
        ISchedulingUnitOfWork unitOfWork,
        ITeacherVerifier teacherVerifier,
        ILogger<UpdateScheduleRequest> logger,
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
            logger.LogWarning("Schedule {ScheduleId} is {Status} and cannot be updated", id, schedule.Status);

            return Results.Problem(
                detail: $"Schedule is {schedule.Status.ToString().ToLowerInvariant()} and cannot be updated.",
                statusCode: StatusCodes.Status409Conflict
            );
        }

        if (request.TeacherId != schedule.TeacherId)
        {
            var teacherExists = await teacherVerifier.ExistsAsync(request.TeacherId, ct);
            if (!teacherExists)
                return Results.Problem(
                    detail: $"Teacher with id '{request.TeacherId}' not found.",
                    statusCode: StatusCodes.Status404NotFound
                );

            if (!await teacherVerifier.IsAvailableAsync(request.TeacherId, ct))
                return Results.Problem(
                    detail: "Teacher is not active and cannot be given lessons.",
                    statusCode: StatusCodes.Status409Conflict
                );
        }

        var hasConflict = await repository.HasTeacherConflictAsync(
            request.TeacherId, schedule.ScheduledDate, request.DurationMinutes, id, ct);

        if (hasConflict)
        {
            logger.LogWarning(
                "Teacher {TeacherId} already has a lesson at {ScheduledDate}",
                request.TeacherId, schedule.ScheduledDate
            );

            return Results.Problem(
                detail: "Teacher already has a lesson at this time.",
                statusCode: StatusCodes.Status409Conflict
            );
        }

        schedule.Update(request.TeacherId, request.DurationMinutes, request.Notes);

        await repository.UpdateAsync(schedule, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Schedule updated: {ScheduleId}", id);

        return Results.NoContent();
    }
}
