using FluentValidation;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Identity.Contracts;
using Microsoft.Extensions.Logging;
using Scheduling.Contracts;
using Teachers.Application.Abstractions;
using Teachers.Domain.Enums;

namespace Teachers.Application.Features.ChangeTeacherStatus;

public sealed record ChangeTeacherStatusRequest(TeacherStatus Status);

// What the status change did to the teacher's calendar and account
public sealed record ChangeTeacherStatusResponse(
    int CancelledLessons,
    int RestoredLessons,
    int SkippedLessons,
    bool AccountActive,
    string? Hint
);

public sealed class ChangeTeacherStatusValidator : AbstractValidator<ChangeTeacherStatusRequest>
{
    public ChangeTeacherStatusValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid teacher status.");
    }
}

public static class ChangeTeacherStatusEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPut("/{id:guid}/status", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanManageTeachers))
             .WithName("ChangeTeacherStatus")
             .WithSummary("Change teacher status")
             .Produces<ChangeTeacherStatusResponse>(StatusCodes.Status200OK)
             .ProducesValidationProblem()
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> Handle(
        Guid id,
        ChangeTeacherStatusRequest request,
        IValidator<ChangeTeacherStatusRequest> validator,
        ITeacherRepository repository,
        ITeacherUnitOfWork unitOfWork,
        IScheduleLifecycle scheduleLifecycle,
        IUserAccountManager accountManager,
        ILogger<ChangeTeacherStatusRequest> logger,
        CancellationToken ct
    )
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
            return Results.ValidationProblem(validationResult.ToDictionary());

        var teacher = await repository.GetByIdAsync(id, ct);
        if (teacher is null)
            return Results.Problem(
                detail: $"Teacher with id '{id}' not found.",
                statusCode: StatusCodes.Status404NotFound
            );

        if (teacher.Status == request.Status)
        {
            logger.LogWarning("Teacher {TeacherId} already has status {Status}", id, request.Status);

            return Results.Problem(
                detail: $"Teacher already has status '{request.Status}'.",
                statusCode: StatusCodes.Status409Conflict
            );
        }

        var wasAvailable = IsAvailable(teacher.Status);

        teacher.ChangeStatus(request.Status);

        await repository.UpdateAsync(teacher, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Teacher {TeacherId} status changed to {Status}", id, request.Status);

        // Only a teacher who left (Resigned, Dismissed) loses access; being on leave keeps the account
        var accountActive = request.Status is not (TeacherStatus.Resigned or TeacherStatus.Dismissed);
        var isAvailable = IsAvailable(request.Status);

        // The status is already saved; a failure in the other modules is reported, not hidden behind a 500
        try
        {
            if (teacher.UserId is { } userId)
                await accountManager.SetActiveAsync(userId, accountActive, ct);

            if (wasAvailable && !isAvailable)
            {
                var cancelled = await scheduleLifecycle.CancelForTeacherAsync(id, ct);

                return Results.Ok(new ChangeTeacherStatusResponse(
                    cancelled, 0, 0, accountActive,
                    cancelled > 0
                        ? "Upcoming lessons were cancelled. Hand them over to another teacher with PUT /api/schedules/reassign-teacher, or they return automatically when this teacher does."
                        : null));
            }

            if (!wasAvailable && isAvailable)
            {
                var restored = await scheduleLifecycle.RestoreForTeacherAsync(id, ct);

                return Results.Ok(new ChangeTeacherStatusResponse(
                    0, restored.RestoredCount, restored.SkippedCount, accountActive,
                    restored.SkippedCount > 0
                        ? "Some lessons could not be restored (time already taken or the student is away)."
                        : null));
            }

            return Results.Ok(new ChangeTeacherStatusResponse(0, 0, 0, accountActive, null));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Teacher {TeacherId} status changed but calendar/account update failed", id);

            return Results.Ok(new ChangeTeacherStatusResponse(
                0, 0, 0, accountActive,
                "The status was saved, but the calendar or account could not be fully updated. Check them manually."));
        }
    }

    private static bool IsAvailable(TeacherStatus status) =>
        status is TeacherStatus.Probation or TeacherStatus.Employed;
}
