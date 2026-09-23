using Enrollments.Contracts;
using FluentValidation;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Scheduling.Application.Abstractions;
using Scheduling.Domain.Enums;
using Teachers.Contracts;
using Shared.Kernel.Abstractions;
using Scheduling.Application.Services;

namespace Scheduling.Application.Features.ReassignTeacher;

// Hands over the upcoming lessons (and, by default, the groups) of one teacher to another one.
// Lessons cancelled because the first teacher became unavailable are brought back.
public sealed record ReassignTeacherRequest(
    Guid FromTeacherId,
    Guid ToTeacherId,
    bool UpdateGroups = true
);

public sealed record ReassignTeacherResponse(
    int ReassignedCount,
    int RestoredCount,
    int UpdatedGroupsCount
);

public sealed class ReassignTeacherValidator : AbstractValidator<ReassignTeacherRequest>
{
    public ReassignTeacherValidator()
    {
        RuleFor(x => x.FromTeacherId)
            .NotEmpty().WithMessage("Current teacher is required.");

        RuleFor(x => x.ToTeacherId)
            .NotEmpty().WithMessage("New teacher is required.")
            .NotEqual(x => x.FromTeacherId).WithMessage("New teacher must differ from the current one.");
    }
}

public static class ReassignTeacherEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPut("/reassign-teacher", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanManageSchedule))
             .WithName("ReassignTeacher")
             .WithSummary("Hand over the upcoming lessons and groups of a teacher to another teacher")
             .Produces<ReassignTeacherResponse>(StatusCodes.Status200OK)
             .ProducesValidationProblem()
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> Handle(
        ReassignTeacherRequest request,
        IValidator<ReassignTeacherRequest> validator,
        IScheduleRepository repository,
        IStudyGroupRepository groupRepository,
        ISchedulingUnitOfWork unitOfWork,
        ITransactionCoordinator transaction,
        ITeacherVerifier teacherVerifier,
        IEnrollmentLookup enrollmentLookup,
        ILogger<ReassignTeacherRequest> logger,
        CancellationToken ct
    )
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
            return Results.ValidationProblem(validationResult.ToDictionary());

        if (!await teacherVerifier.ExistsAsync(request.FromTeacherId, ct))
            return Results.Problem(
                detail: $"Teacher with id '{request.FromTeacherId}' not found.",
                statusCode: StatusCodes.Status404NotFound
            );

        if (!await teacherVerifier.ExistsAsync(request.ToTeacherId, ct))
            return Results.Problem(
                detail: $"Teacher with id '{request.ToTeacherId}' not found.",
                statusCode: StatusCodes.Status404NotFound
            );

        if (!await teacherVerifier.IsAvailableAsync(request.ToTeacherId, ct))
            return Results.Problem(
                detail: "The new teacher is not active and cannot be given lessons.",
                statusCode: StatusCodes.Status409Conflict
            );

        return await transaction.ExecuteAsync<IResult>(async token =>
        {
            // Lessons leave one calendar and enter another: both are held until the change is saved
            await transaction.AcquireTeacherCalendarLocksAsync([request.FromTeacherId, request.ToTeacherId], token);

            var now = DateTime.UtcNow;

            var open = await repository.GetFutureOpenByTeacherAsync(request.FromTeacherId, now, ct);
            var cancelled = await repository.GetFutureCancelledAsync(
                null, request.FromTeacherId, CancellationReason.TeacherUnavailable, now, ct);

            // A lesson of a student who is away stays cancelled, only its teacher changes
            var enrollments = (await enrollmentLookup.GetByIdsAsync(
                    cancelled.Where(l => l.EnrollmentId.HasValue).Select(l => l.EnrollmentId!.Value).Distinct().ToList(), ct))
                .ToDictionary(e => e.Id);

            var toRestore = cancelled
                .Where(l => l.GroupId.HasValue
                    || (l.EnrollmentId is { } id && enrollments.TryGetValue(id, out var e) && e.IsActive))
                .ToList();

            // Everything that will be held by the new teacher must fit into their calendar
            var willBeHeld = open.Concat(toRestore).OrderBy(l => l.ScheduledDate).ToList();

            if (willBeHeld.Count > 0)
            {
                var busy = await repository.GetOpenByTeacherInRangeAsync(
                    request.ToTeacherId, willBeHeld[0].ScheduledDate, willBeHeld[^1].EndDate, ct);

                var conflicts = willBeHeld
                    .Where(l => busy.Any(b => b.ScheduledDate < l.EndDate && b.EndDate > l.ScheduledDate))
                    .ToList();

                for (var i = 1; i < willBeHeld.Count; i++)
                    if (willBeHeld[i].ScheduledDate < willBeHeld[i - 1].EndDate && !conflicts.Contains(willBeHeld[i]))
                        conflicts.Add(willBeHeld[i]);

                if (conflicts.Count > 0)
                {
                    logger.LogWarning(
                        "Reassignment from {From} to {To} blocked: {Count} conflicting lessons",
                        request.FromTeacherId, request.ToTeacherId, conflicts.Count);

                    var shown = string.Join(", ", conflicts.Take(5).Select(c => c.ScheduledDate.ToString("yyyy-MM-dd HH:mm 'UTC'")));

                    return Results.Problem(
                        detail: $"The new teacher is busy at {conflicts.Count} of these times (e.g. {shown}). Nothing was changed.",
                        statusCode: StatusCodes.Status409Conflict
                    );
                }
            }

            foreach (var lesson in open.Concat(cancelled))
                lesson.ReassignTeacher(request.ToTeacherId);

            foreach (var lesson in toRestore)
                lesson.RestoreIfSystemCancelled();

            // Lessons of an inactive student now belong to the new teacher but wait for the student
            foreach (var lesson in cancelled.Except(toRestore))
                lesson.Cancel(CancellationReason.EnrollmentInactive);

            var updatedGroups = 0;

            if (request.UpdateGroups)
            {
                var groups = await groupRepository.GetByTeacherAsync(request.FromTeacherId, ct);

                foreach (var studyGroup in groups)
                    studyGroup.ChangeTeacher(request.ToTeacherId);

                updatedGroups = groups.Count;
            }

            await unitOfWork.SaveChangesAsync(ct);

            logger.LogInformation(
                "Reassigned {Count} lessons ({Restored} restored) and {Groups} groups from {From} to {To}",
                open.Count + cancelled.Count, toRestore.Count, updatedGroups, request.FromTeacherId, request.ToTeacherId);

            return Results.Ok(new ReassignTeacherResponse(open.Count + cancelled.Count, toRestore.Count, updatedGroups));
        }, ct);
    }
}
