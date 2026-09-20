using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Scheduling.Application.Abstractions;

namespace Scheduling.Application.Features.CompleteSchedule;

public sealed record CompleteScheduleRequest;

public static class CompleteScheduleEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPut("/{id:guid}/complete", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanManageSchedule))
             .WithName("CompleteSchedule")
             .WithSummary("Complete a schedule")
             .Produces(StatusCodes.Status204NoContent)
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> Handle(
        Guid id,
        IScheduleRepository repository,
        ISchedulingUnitOfWork unitOfWork,
        ILogger<CompleteScheduleRequest> logger,
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
            logger.LogWarning("Schedule {ScheduleId} is {Status} and cannot be completed", id, schedule.Status);

            return Results.Problem(
                detail: $"Schedule is {schedule.Status.ToString().ToLowerInvariant()} and cannot be completed.",
                statusCode: StatusCodes.Status409Conflict
            );
        }

        schedule.Complete();

        await repository.UpdateAsync(schedule, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Schedule completed: {ScheduleId}", id);

        return Results.NoContent();
    }
}
