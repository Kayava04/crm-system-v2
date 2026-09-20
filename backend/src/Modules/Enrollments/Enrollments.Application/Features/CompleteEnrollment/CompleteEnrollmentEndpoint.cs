using Enrollments.Application.Abstractions;
using Enrollments.Domain.Enums;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Scheduling.Contracts;

namespace Enrollments.Application.Features.CompleteEnrollment;

public sealed record CompleteEnrollmentRequest;

public static class CompleteEnrollmentEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPut("/{id:guid}/complete", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanManageEnrollments))
             .WithName("CompleteEnrollment")
             .WithSummary("Complete an enrollment and cancel its remaining upcoming lessons")
             .Produces<EnrollmentStatusChangeResponse>(StatusCodes.Status200OK)
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> Handle(
        Guid id,
        IEnrollmentRepository repository,
        IEnrollmentUnitOfWork unitOfWork,
        IScheduleLifecycle scheduleLifecycle,
        ILogger<CompleteEnrollmentRequest> logger,
        CancellationToken ct
    )
    {
        var enrollment = await repository.GetByIdAsync(id, ct);
        if (enrollment is null)
            return Results.Problem(
                detail: $"Enrollment with id '{id}' not found.",
                statusCode: StatusCodes.Status404NotFound
            );

        if (enrollment.Status == EnrollmentStatus.Completed)
            return Results.Problem(
                detail: "Enrollment is already completed.",
                statusCode: StatusCodes.Status409Conflict
            );

        enrollment.Complete();

        await repository.UpdateAsync(enrollment, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Enrollment completed: {EnrollmentId}", id);

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
