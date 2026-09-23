using FluentValidation;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Scheduling.Application.Abstractions;
using Scheduling.Application.Services;
using Scheduling.Domain.Entities;
using Scheduling.Domain.Enums;
using Teachers.Contracts;
using Shared.Kernel.Abstractions;

namespace Scheduling.Application.Features.CreateSchedule;

// Exactly one of EnrollmentId (individual course) and GroupId (group course) must be set
public sealed record CreateScheduleRequest(
    Guid? EnrollmentId,
    Guid? GroupId,
    Guid TeacherId,
    DateTime ScheduledDate,
    int DurationMinutes,
    string? Notes
);

public sealed record CreateScheduleResponse(
    Guid Id,
    Guid? EnrollmentId,
    Guid? GroupId,
    Guid TeacherId,
    DateTime ScheduledDate,
    int DurationMinutes,
    ScheduleStatus Status
);

public sealed class CreateScheduleValidator : AbstractValidator<CreateScheduleRequest>
{
    public CreateScheduleValidator()
    {
        RuleFor(x => x)
            .Must(x => x.EnrollmentId is null != x.GroupId is null)
            .WithName("EnrollmentId")
            .WithMessage("Specify either an enrollment or a study group.");

        // NotEmpty() on a nullable Guid only rejects null, not Guid.Empty - an explicit
        // comparison is needed here to actually catch an empty-but-present id.
        RuleFor(x => x.EnrollmentId)
            .Must(id => id != Guid.Empty).WithMessage("Enrollment must not be empty.")
            .When(x => x.EnrollmentId is not null);

        RuleFor(x => x.GroupId)
            .Must(id => id != Guid.Empty).WithMessage("Study group must not be empty.")
            .When(x => x.GroupId is not null);

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
             .WithSummary("Create a single lesson for an enrollment or a study group")
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
        ITransactionCoordinator transaction,
        LessonTargetResolver targetResolver,
        ITeacherVerifier teacherVerifier,
        ILogger<CreateScheduleRequest> logger,
        CancellationToken ct
    )
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
            return Results.ValidationProblem(validationResult.ToDictionary());

        var resolution = await targetResolver.ResolveAsync(request.EnrollmentId, request.GroupId, ct);
        if (resolution.Error is not null)
            return resolution.Error;

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

        var scheduledDate = request.ScheduledDate.ToUniversalTime();

        return await transaction.ExecuteAsync<IResult>(async token =>
        {
            // Two requests for the same teacher and time must not both pass the conflict check below
            await transaction.AcquireTeacherCalendarLocksAsync([request.TeacherId], token);

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

            var schedule = request.EnrollmentId is not null
                ? Schedule.CreateForEnrollment(
                    request.EnrollmentId.Value, request.TeacherId, scheduledDate, request.DurationMinutes, request.Notes)
                : Schedule.CreateForGroup(
                    request.GroupId!.Value, request.TeacherId, scheduledDate, request.DurationMinutes, request.Notes);

            await repository.AddAsync(schedule, ct);
            await unitOfWork.SaveChangesAsync(ct);

            logger.LogInformation(
                "Schedule created: {ScheduleId} for Enrollment {EnrollmentId} / Group {GroupId}",
                schedule.Id, request.EnrollmentId, request.GroupId
            );

            var response = new CreateScheduleResponse(
                schedule.Id,
                schedule.EnrollmentId,
                schedule.GroupId,
                schedule.TeacherId,
                schedule.ScheduledDate,
                schedule.DurationMinutes,
                schedule.Status
            );

            return Results.Created($"/api/schedules/{schedule.Id}", response);
        }, ct);
    }
}
