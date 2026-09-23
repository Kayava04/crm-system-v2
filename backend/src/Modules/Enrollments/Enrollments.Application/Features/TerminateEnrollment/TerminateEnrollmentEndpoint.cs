using Enrollments.Application.Abstractions;
using Enrollments.Domain.Enums;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Scheduling.Contracts;
using Shared.Kernel.Abstractions;

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
        ITransactionCoordinator transaction,
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

        // The new status and its effect on the calendar are saved together or not at all
        var response = await transaction.ExecuteAsync(async token =>
        {
            enrollment.Terminate();

            await repository.UpdateAsync(enrollment, token);
            await unitOfWork.SaveChangesAsync(token);

            var cancelled = await scheduleLifecycle.CancelForEnrollmentAsync(id, token);

            return new EnrollmentStatusChangeResponse(cancelled, 0, 0, 0);
        }, ct);

        logger.LogInformation("Enrollment terminated: {EnrollmentId}", id);

        return Results.Ok(response);
    }
}
