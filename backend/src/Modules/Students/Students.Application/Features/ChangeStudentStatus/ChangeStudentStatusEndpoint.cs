using FluentValidation;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Enrollments.Contracts;
using Identity.Contracts;
using Microsoft.Extensions.Logging;
using Shared.Kernel.Abstractions;
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
    bool AccountActive
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
        ITransactionCoordinator transaction,
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

        // Only a student who left (Withdrawn) loses access; a pause or graduation keeps the account
        var accountActive = request.Status != StudentStatus.Withdrawn;

        // Status, account, enrollments and calendar change together or not at all
        var response = await transaction.ExecuteAsync(async token =>
        {
            student.ChangeStatus(request.Status);

            await repository.UpdateAsync(student, token);
            await unitOfWork.SaveChangesAsync(token);

            if (student.UserId is { } userId)
                await accountManager.SetActiveAsync(userId, accountActive, token);

            if (request.Status == StudentStatus.Active)
            {
                var resumed = await enrollmentLifecycle.RestoreForStudentAsync(id, token);

                return new ChangeStudentStatusResponse(
                    0, 0, resumed.EnrollmentsCount, resumed.RestoredLessons, resumed.LessonsLeftToSchedule, accountActive);
            }

            if (wasActive)
            {
                var suspended = await enrollmentLifecycle.SuspendForStudentAsync(id, token);

                return new ChangeStudentStatusResponse(
                    suspended.EnrollmentsCount, suspended.CancelledLessons, 0, 0, 0, accountActive);
            }

            return new ChangeStudentStatusResponse(0, 0, 0, 0, 0, accountActive);
        }, ct);

        logger.LogInformation("Student {StudentId} status changed to {Status}", id, request.Status);

        return Results.Ok(response);
    }
}
