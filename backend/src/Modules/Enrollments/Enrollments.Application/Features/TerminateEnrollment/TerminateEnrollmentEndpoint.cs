using Enrollments.Application.Abstractions;
using Enrollments.Domain.Enums;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Scheduling.Contracts;

namespace Enrollments.Application.Features.TerminateEnrollment;

public sealed record TerminateEnrollmentRequest;

public static class TerminateEnrollmentEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPut("/{id:guid}/terminate", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanManageEnrollments))
             .WithName("TerminateEnrollment")
             .WithSummary("Terminate an enrollment and cancel its upcoming lessons")
             .Produces<EnrollmentStatusChangeResponse>(StatusCodes.Status200OK)
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> Handle(
        Guid id,
        IEnrollmentRepository repository,
        IEnrollmentUnitOfWork unitOfWork,
        IScheduleLifecycle scheduleLifecycle,
        ILogger<TerminateEnrollmentRequest> logger,
        CancellationToken ct
    )
    {
        var enrollment = await repository.GetByIdAsync(id, ct);
        if (enrollment is null)
            return Results.Problem(
                detail: $"Enrollment with id '{id}' not found.",
                statusCode: StatusCodes.Status404NotFound
            );

        if (enrollment.Status == EnrollmentStatus.Terminated)
            return Results.Problem(
                detail: "Enrollment is already terminated.",
                statusCode: StatusCodes.Status409Conflict
            );

        enrollment.Terminate();

        await repository.UpdateAsync(enrollment, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Enrollment terminated: {EnrollmentId}", id);

        // The enrollment is already saved; a calendar failure must not hide that, so it is reported instead
        try
        {
            var cancelled = await scheduleLifecycle.CancelForEnrollmentAsync(id, ct);

            return Results.Ok(new EnrollmentStatusChangeResponse(cancelled, 0, 0, 0, null));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Enrollment {EnrollmentId} status changed but the calendar update failed", id);

            return Results.Ok(new EnrollmentStatusChangeResponse(
                0, 0, 0, 0, "The enrollment was updated, but its calendar could not be updated. Check the lessons manually."));
        }
    }
}
