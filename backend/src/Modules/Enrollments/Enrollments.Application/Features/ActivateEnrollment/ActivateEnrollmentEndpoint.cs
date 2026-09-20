using Enrollments.Application.Abstractions;
using Enrollments.Domain.Enums;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Scheduling.Contracts;
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

        enrollment.Activate();

        await repository.UpdateAsync(enrollment, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Enrollment activated: {EnrollmentId}", id);

        // The enrollment is already saved; a calendar failure must not hide that, so it is reported instead
        try
        {
            var restored = await scheduleLifecycle.RestoreForEnrollmentAsync(id, ct);

            return Results.Ok(new EnrollmentStatusChangeResponse(
                0, restored.RestoredCount, restored.SkippedCount, restored.LessonsLeftToSchedule, null));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Enrollment {EnrollmentId} status changed but the calendar update failed", id);

            return Results.Ok(new EnrollmentStatusChangeResponse(
                0, 0, 0, 0, "The enrollment was updated, but its calendar could not be updated. Check the lessons manually."));
        }
    }
}
