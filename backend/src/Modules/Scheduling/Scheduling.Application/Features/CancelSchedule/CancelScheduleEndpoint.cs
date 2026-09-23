using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Scheduling.Application.Abstractions;

namespace Scheduling.Application.Features.CancelSchedule;

public sealed record CancelScheduleRequest;

public static class CancelScheduleEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPut("/{id:guid}/cancel", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanManageSchedule))
             .WithName("CancelSchedule")
             .WithSummary("Cancel a schedule")
             .Produces(StatusCodes.Status204NoContent)
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> Handle(
        Guid id,
        IScheduleRepository repository,
        ISchedulingUnitOfWork unitOfWork,
        ILogger<CancelScheduleRequest> logger,
        CancellationToken ct
    )
    {
        var schedule = await repository.GetByIdAsync(id, ct);
        if (schedule is null)
            return Results.Problem(
                detail: $"Schedule with id '{id}' not found.",
                statusCode: StatusCodes.Status404NotFound
            );

        if (!schedule.IsOpen)
        {
            logger.LogWarning("Schedule {ScheduleId} is {Status} and cannot be cancelled", id, schedule.Status);

            return Results.Problem(
                detail: $"Schedule is {schedule.Status.ToString().ToLowerInvariant()} and cannot be cancelled.",
                statusCode: StatusCodes.Status409Conflict
            );
        }

        schedule.Cancel();

        await repository.UpdateAsync(schedule, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Schedule cancelled: {ScheduleId}", id);

        return Results.NoContent();
    }
}
