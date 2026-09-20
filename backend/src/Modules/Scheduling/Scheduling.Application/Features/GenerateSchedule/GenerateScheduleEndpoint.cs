using FluentValidation;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Scheduling.Application.Abstractions;
using Scheduling.Application.Services;
using Scheduling.Domain.Entities;
using Teachers.Contracts;

namespace Scheduling.Application.Features.GenerateSchedule;

// One weekly slot; StartTime is local school time in "HH:mm" format
public sealed record LessonSlot(DayOfWeek DayOfWeek, string StartTime);

// Exactly one of EnrollmentId and GroupId must be set. TeacherId is required for an enrollment
// and defaults to the group's teacher for a group.
public sealed record GenerateScheduleRequest(
    Guid? EnrollmentId,
    Guid? GroupId,
    Guid? TeacherId,
    List<LessonSlot> Slots,
    int DurationMinutes,
    DateOnly? StartDate,
    int? LessonsCount
);

public sealed record GenerateScheduleResponse(
    int CreatedCount,
    DateTime FirstLessonAt,
    DateTime LastLessonAt,
    string TimeZone
);

public sealed class GenerateScheduleValidator : AbstractValidator<GenerateScheduleRequest>
{
    public GenerateScheduleValidator()
    {
        RuleFor(x => x)
            .Must(x => x.EnrollmentId is null != x.GroupId is null)
            .WithName("EnrollmentId")
            .WithMessage("Specify either an enrollment or a study group.");

        RuleFor(x => x.TeacherId)
            .NotEmpty().WithMessage("Teacher is required for an enrollment.")
            .When(x => x.EnrollmentId is not null);

        RuleFor(x => x.Slots)
            .NotEmpty().WithMessage("At least one weekly slot is required.")
            .Must(s => s.Count <= 7).WithMessage("At most 7 weekly slots are allowed.")
            .Must(s => s.Select(x => x.DayOfWeek).Distinct().Count() == s.Count)
            .WithMessage("Each day of the week can appear only once.");

        RuleForEach(x => x.Slots).ChildRules(slot =>
        {
            slot.RuleFor(s => s.DayOfWeek)
                .IsInEnum().WithMessage("Invalid day of week.");

            slot.RuleFor(s => s.StartTime)
                .Must(t => TimeOnly.TryParseExact(t, "HH:mm", out _))
                .WithMessage("Start time must be in HH:mm format.");
        });

        RuleFor(x => x.DurationMinutes)
            .InclusiveBetween(15, 480).WithMessage("Duration must be between 15 and 480 minutes.");

        RuleFor(x => x.LessonsCount)
            .InclusiveBetween(1, 200).WithMessage("Lessons count must be between 1 and 200.")
            .When(x => x.LessonsCount.HasValue);
    }
}

public static class GenerateScheduleEndpoint
{
    // Safety net so an unreachable lessons count cannot loop forever
    private const int MaxDaysAhead = 3 * 365;

    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/generate", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanManageSchedule))
             .WithName("GenerateSchedule")
             .WithSummary("Generate the recurring lessons of an enrollment or a study group from weekly slots")
             .Produces<GenerateScheduleResponse>(StatusCodes.Status201Created)
             .ProducesValidationProblem()
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> Handle(
        GenerateScheduleRequest request,
        IValidator<GenerateScheduleRequest> validator,
        IScheduleRepository repository,
        ISchedulingUnitOfWork unitOfWork,
        LessonTargetResolver targetResolver,
        ITeacherVerifier teacherVerifier,
        ISchoolClock clock,
        ILogger<GenerateScheduleRequest> logger,
        CancellationToken ct
    )
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
            return Results.ValidationProblem(validationResult.ToDictionary());

        var resolution = await targetResolver.ResolveAsync(request.EnrollmentId, request.GroupId, ct);
        if (resolution.Error is not null)
            return resolution.Error;

        var target = resolution.Target!;

        var teacherId = request.TeacherId ?? target.DefaultTeacherId!.Value;
        var teacherExists = await teacherVerifier.ExistsAsync(teacherId, ct);
        if (!teacherExists)
            return Results.Problem(
                detail: $"Teacher with id '{teacherId}' not found.",
                statusCode: StatusCodes.Status404NotFound
            );

        if (!await teacherVerifier.IsAvailableAsync(teacherId, ct))
            return Results.Problem(
                detail: "Teacher is not active and cannot be given lessons.",
                statusCode: StatusCodes.Status409Conflict
            );

        var alreadyScheduled = await repository.CountActiveByTargetAsync(target.EnrollmentId, target.GroupId, ct);
        var lessonsToCreate = request.LessonsCount ?? target.Course.LessonsCount - alreadyScheduled;

        if (lessonsToCreate <= 0)
        {
            logger.LogWarning(
                "Nothing to generate for Enrollment {EnrollmentId} / Group {GroupId}: {Scheduled} of {Total} lessons already scheduled",
                target.EnrollmentId, target.GroupId, alreadyScheduled, target.Course.LessonsCount
            );

            return Results.Problem(
                detail: "All lessons of the course are already scheduled.",
                statusCode: StatusCodes.Status409Conflict
            );
        }

        var slots = request.Slots
            .Select(s => (s.DayOfWeek, Time: TimeOnly.ParseExact(s.StartTime, "HH:mm")))
            .ToDictionary(s => s.DayOfWeek, s => s.Time);

        var today = clock.Today;
        var firstDate = request.StartDate ?? target.EnrollmentStartDate ?? today;
        if (firstDate < today)
            firstDate = today;

        var starts = new List<DateTime>();

        for (var offset = 0; offset < MaxDaysAhead && starts.Count < lessonsToCreate; offset++)
        {
            var date = firstDate.AddDays(offset);

            if (!slots.TryGetValue(date.DayOfWeek, out var time))
                continue;

            var startUtc = clock.ToUtc(date, time);

            if (startUtc > DateTime.UtcNow)
                starts.Add(startUtc);
        }

        if (starts.Count < lessonsToCreate)
            return Results.Problem(
                detail: "Could not fit all lessons into the next 3 years with the given slots.",
                statusCode: StatusCodes.Status409Conflict
            );

        var duration = TimeSpan.FromMinutes(request.DurationMinutes);

        var busy = await repository.GetOpenByTeacherInRangeAsync(
            teacherId, starts[0], starts[^1] + duration, ct);

        var conflicts = starts
            .Where(start => busy.Any(b => b.ScheduledDate < start + duration && b.EndDate > start))
            .ToList();

        if (conflicts.Count > 0)
        {
            logger.LogWarning(
                "Schedule generation blocked: teacher {TeacherId} is busy at {ConflictCount} of the requested times",
                teacherId, conflicts.Count
            );

            var shown = string.Join(", ", conflicts.Take(5).Select(c => c.ToString("yyyy-MM-dd HH:mm 'UTC'")));

            return Results.Problem(
                detail: $"Teacher already has lessons at {conflicts.Count} of the requested times (e.g. {shown}). Nothing was created.",
                statusCode: StatusCodes.Status409Conflict
            );
        }

        var schedules = starts
            .Select(start => target.EnrollmentId is not null
                ? Schedule.CreateForEnrollment(target.EnrollmentId.Value, teacherId, start, request.DurationMinutes)
                : Schedule.CreateForGroup(target.GroupId!.Value, teacherId, start, request.DurationMinutes))
            .ToList();

        await repository.AddRangeAsync(schedules, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Generated {Count} lessons for Enrollment {EnrollmentId} / Group {GroupId}",
            schedules.Count, target.EnrollmentId, target.GroupId
        );

        var response = new GenerateScheduleResponse(schedules.Count, starts[0], starts[^1], clock.TimeZoneId);

        return Results.Created(
            target.EnrollmentId is not null
                ? $"/api/schedules?enrollmentId={target.EnrollmentId}"
                : $"/api/schedules?groupId={target.GroupId}",
            response);
    }
}
