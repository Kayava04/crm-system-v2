using Enrollments.Contracts;
using FluentValidation;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Scheduling.Application.Abstractions;
using Scheduling.Domain.Entities;
using Scheduling.Domain.Enums;
using Teachers.Contracts;

namespace Scheduling.Application.Features.CreateSchedule;

public sealed record CreateScheduleRequest(
    Guid EnrollmentId,
    Guid TeacherId,
    DateTime ScheduledDate,
    int DurationMinutes,
    string? Notes
);

public sealed record CreateScheduleResponse(
    Guid Id,
    Guid EnrollmentId,
    Guid TeacherId,
    DateTime ScheduledDate,
    int DurationMinutes,
    ScheduleStatus Status
);

public sealed class CreateScheduleValidator : AbstractValidator<CreateScheduleRequest>
{
    public CreateScheduleValidator()
    {
        RuleFor(x => x.EnrollmentId)
            .NotEmpty().WithMessage("Enrollment is required.");

        RuleFor(x => x.TeacherId)
            .NotEmpty().WithMessage("Teacher is required.");

        RuleFor(x => x.ScheduledDate)
            .NotEmpty().WithMessage("Scheduled date is required.")
            .Must(d => d.ToUniversalTime() > DateTime.UtcNow)
            .WithMessage("Scheduled date must be in the future.");

        RuleFor(x => x.DurationMinutes)
            .InclusiveBetween(15, 480).WithMessage("Duration must be between 15 and 480 minutes.");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Notes must not exceed 500 characters.")
            .When(x => x.Notes is not null);
    }
}

public static class CreateEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanManageSchedule))
             .WithName("CreateSchedule")
             .WithSummary("Create a schedule")
             .Produces<CreateScheduleResponse>(StatusCodes.Status201Created)
             .ProducesValidationProblem()
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> Handle(
        CreateScheduleRequest request,
        IValidator<CreateScheduleRequest> validator,
        IScheduleRepository repository,
        ISchedulingUnitOfWork unitOfWork,
        IEnrollmentLookup enrollmentLookup,
        ITeacherVerifier teacherVerifier,
        ILogger<CreateScheduleRequest> logger,
        CancellationToken ct
    )
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
            return Results.ValidationProblem(validationResult.ToDictionary());

        var enrollment = await enrollmentLookup.GetByIdAsync(request.EnrollmentId, ct);
        if (enrollment is null)
            return Results.Problem(
                detail: $"Enrollment with id '{request.EnrollmentId}' not found.",
                statusCode: StatusCodes.Status404NotFound
            );

        var teacherExists = await teacherVerifier.ExistsAsync(request.TeacherId, ct);
        if (!teacherExists)
            return Results.Problem(
                detail: $"Teacher with id '{request.TeacherId}' not found.",
                statusCode: StatusCodes.Status404NotFound
            );

        if (!enrollment.IsActive)
        {
            logger.LogWarning("Enrollment {EnrollmentId} is not active", request.EnrollmentId);

            return Results.Problem(
                detail: "Lessons can only be scheduled for an active enrollment.",
                statusCode: StatusCodes.Status409Conflict
            );
        }

        var scheduledDate = request.ScheduledDate.ToUniversalTime();

        var hasConflict = await repository.HasTeacherConflictAsync(
            request.TeacherId, scheduledDate, request.DurationMinutes, ct: ct);

        if (hasConflict)
        {
            logger.LogWarning(
                "Teacher {TeacherId} already has a lesson at {ScheduledDate}",
                request.TeacherId, scheduledDate
            );

            return Results.Problem(
                detail: "Teacher already has a lesson at this time.",
                statusCode: StatusCodes.Status409Conflict
            );
        }

        var schedule = Schedule.Create(
            request.EnrollmentId,
            request.TeacherId,
            scheduledDate,
            request.DurationMinutes,
            request.Notes
        );

        await repository.AddAsync(schedule, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Schedule created: {ScheduleId} for Enrollment {EnrollmentId}",
            schedule.Id, request.EnrollmentId
        );

        var response = new CreateScheduleResponse(
            schedule.Id,
            schedule.EnrollmentId,
            schedule.TeacherId,
            schedule.ScheduledDate,
            schedule.DurationMinutes,
            schedule.Status
        );

        return Results.Created($"/api/schedules/{schedule.Id}", response);
    }
}
