using Enrollments.Application.Abstractions;
using Enrollments.Domain.Enums;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Scheduling.Contracts;
using Shared.Kernel.Abstractions;
using Students.Contracts;

namespace Enrollments.Application.Features.ActivateEnrollment;

public sealed record ActivateEnrollmentRequest;

public static class ActivateEnrollmentEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPut("/{id:guid}/activate", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanManageEnrollments))
             .WithName("ActivateEnrollment")
             .WithSummary("Activate an enrollment and bring back its upcoming lessons")
             .Produces<EnrollmentStatusChangeResponse>(StatusCodes.Status200OK)
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> Handle(
        Guid id,
        IEnrollmentRepository repository,
        IEnrollmentUnitOfWork unitOfWork,
        ITransactionCoordinator transaction,
        IScheduleLifecycle scheduleLifecycle,
        IStudentVerifier studentVerifier,
        ILogger<ActivateEnrollmentRequest> logger,
        CancellationToken ct
    )
    {
        var enrollment = await repository.GetByIdAsync(id, ct);
        if (enrollment is null)
            return Results.Problem(
                detail: $"Enrollment with id '{id}' not found.",
                statusCode: StatusCodes.Status404NotFound
            );

        if (enrollment.Status == EnrollmentStatus.Active)
            return Results.Problem(
                detail: "Enrollment is already active.",
                statusCode: StatusCodes.Status409Conflict
            );

        if (!await studentVerifier.IsActiveAsync(enrollment.StudentId, ct))
        {
            logger.LogWarning("Enrollment {EnrollmentId} cannot be activated: student is not active", id);

            return Results.Problem(
                detail: "The student is not active. Reactivate the student first.",
                statusCode: StatusCodes.Status409Conflict
            );
        }

        // The new status and its effect on the calendar are saved together or not at all
        var response = await transaction.ExecuteAsync(async token =>
        {
            enrollment.Activate();

            await repository.UpdateAsync(enrollment, token);
            await unitOfWork.SaveChangesAsync(token);

            var restored = await scheduleLifecycle.RestoreForEnrollmentAsync(id, token);

            return new EnrollmentStatusChangeResponse(
                0, restored.RestoredCount, restored.SkippedCount, restored.LessonsLeftToSchedule);
        }, ct);

        logger.LogInformation("Enrollment activated: {EnrollmentId}", id);

        return Results.Ok(response);
    }
}
