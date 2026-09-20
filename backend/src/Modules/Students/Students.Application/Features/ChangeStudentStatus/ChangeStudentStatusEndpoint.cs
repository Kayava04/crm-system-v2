using FluentValidation;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Enrollments.Contracts;
using Identity.Contracts;
using Microsoft.Extensions.Logging;
using Students.Application.Abstractions;
using Students.Domain.Enums;

namespace Students.Application.Features.ChangeStudentStatus;

public sealed record ChangeStudentStatusRequest(StudentStatus Status);

// What the status change did to the student's enrollments and calendar
public sealed record ChangeStudentStatusResponse(
    int SuspendedEnrollments,
    int CancelledLessons,
    int ResumedEnrollments,
    int RestoredLessons,
    int LessonsLeftToSchedule,
    bool AccountActive,
    string? Warning
);

public sealed class ChangeStudentStatusValidator : AbstractValidator<ChangeStudentStatusRequest>
{
    public ChangeStudentStatusValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid student status.");
    }
}

public static class ChangeStudentStatusEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPut("/{id:guid}/status", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanManageStudents))
             .WithName("ChangeStudentStatus")
             .WithSummary("Change student status")
             .Produces<ChangeStudentStatusResponse>(StatusCodes.Status200OK)
             .ProducesValidationProblem()
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> Handle(
        Guid id,
        ChangeStudentStatusRequest request,
        IValidator<ChangeStudentStatusRequest> validator,
        IStudentRepository repository,
        IStudentUnitOfWork unitOfWork,
        IEnrollmentLifecycle enrollmentLifecycle,
        IUserAccountManager accountManager,
        ILogger<ChangeStudentStatusRequest> logger,
        CancellationToken ct
    )
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
            return Results.ValidationProblem(validationResult.ToDictionary());

        var student = await repository.GetByIdAsync(id, ct);
        if (student is null)
            return Results.Problem(
                detail: $"Student with id '{id}' not found.",
                statusCode: StatusCodes.Status404NotFound
            );

        if (student.Status == request.Status)
        {
            logger.LogWarning("Student {StudentId} already has status {Status}", id, request.Status);

            return Results.Problem(
                detail: $"Student already has status '{request.Status}'.",
                statusCode: StatusCodes.Status409Conflict
            );
        }

        var wasActive = student.Status == StudentStatus.Active;

        student.ChangeStatus(request.Status);

        await repository.UpdateAsync(student, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Student {StudentId} status changed to {Status}", id, request.Status);

        // Only a student who left (Withdrawn) loses access; a pause or graduation keeps the account
        var accountActive = request.Status != StudentStatus.Withdrawn;

        // The status is already saved; a failure in the other modules is reported, not hidden behind a 500
        try
        {
            if (student.UserId is { } userId)
                await accountManager.SetActiveAsync(userId, accountActive, ct);

            if (request.Status == StudentStatus.Active)
            {
                var resumed = await enrollmentLifecycle.RestoreForStudentAsync(id, ct);

                return Results.Ok(new ChangeStudentStatusResponse(
                    0, 0, resumed.EnrollmentsCount, resumed.RestoredLessons, resumed.LessonsLeftToSchedule, accountActive, null));
            }

            if (wasActive)
            {
                var suspended = await enrollmentLifecycle.SuspendForStudentAsync(id, ct);

                return Results.Ok(new ChangeStudentStatusResponse(
                    suspended.EnrollmentsCount, suspended.CancelledLessons, 0, 0, 0, accountActive, null));
            }

            return Results.Ok(new ChangeStudentStatusResponse(0, 0, 0, 0, 0, accountActive, null));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Student {StudentId} status changed but enrollments/calendar/account update failed", id);

            return Results.Ok(new ChangeStudentStatusResponse(
                0, 0, 0, 0, 0, accountActive,
                "The status was saved, but the enrollments, calendar or account could not be fully updated. Check them manually."));
        }
    }
}
