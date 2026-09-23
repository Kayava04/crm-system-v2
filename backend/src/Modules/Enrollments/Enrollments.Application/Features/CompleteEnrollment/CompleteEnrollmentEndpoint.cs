using Enrollments.Application.Abstractions;
using Enrollments.Domain.Enums;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Scheduling.Contracts;
using Shared.Kernel.Abstractions;

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
        ITransactionCoordinator transaction,
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

        // The new status and its effect on the calendar are saved together or not at all
        var response = await transaction.ExecuteAsync(async token =>
        {
            enrollment.Complete();

            await repository.UpdateAsync(enrollment, token);
            await unitOfWork.SaveChangesAsync(token);

            var cancelled = await scheduleLifecycle.CancelForEnrollmentAsync(id, token);

            return new EnrollmentStatusChangeResponse(cancelled, 0, 0, 0);
        }, ct);

        logger.LogInformation("Enrollment completed: {EnrollmentId}", id);

        return Results.Ok(response);
    }
}
