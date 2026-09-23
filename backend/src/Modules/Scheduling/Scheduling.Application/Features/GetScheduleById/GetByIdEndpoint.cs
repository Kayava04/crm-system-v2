using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Scheduling.Application.Abstractions;
using Scheduling.Domain.Enums;

namespace Scheduling.Application.Features.GetScheduleById;

public sealed record ScheduleDetailResponse(
    Guid Id,
    Guid? EnrollmentId,
    Guid? GroupId,
    Guid TeacherId,
    DateTime ScheduledDate,
    int DurationMinutes,
    ScheduleStatus Status,
    string? Notes,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public static class GetByIdEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/{id:guid}", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanViewSchedule))
             .WithName("GetScheduleById")
             .WithSummary("Get schedule by id")
             .Produces<ScheduleDetailResponse>(StatusCodes.Status200OK)
             .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        Guid id,
        IScheduleRepository repository,
        CancellationToken ct
    )
    {
        var schedule = await repository.GetByIdAsync(id, ct);
        if (schedule is null)
            return Results.Problem(
                detail: $"Schedule with id '{id}' not found.",
                statusCode: StatusCodes.Status404NotFound
            );

        var response = new ScheduleDetailResponse(
            schedule.Id,
            schedule.EnrollmentId,
            schedule.GroupId,
            schedule.TeacherId,
            schedule.ScheduledDate,
            schedule.DurationMinutes,
            schedule.Status,
            schedule.Notes,
            schedule.CreatedAt,
            schedule.UpdatedAt
        );

        return Results.Ok(response);
    }
}
