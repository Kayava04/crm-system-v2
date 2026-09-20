using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Scheduling.Application.Abstractions;
using Scheduling.Domain.Enums;
using Shared.Kernel.Common;

namespace Scheduling.Application.Features.GetSchedules;

public sealed record ScheduleListResponse(
    Guid Id,
    Guid? EnrollmentId,
    Guid? GroupId,
    Guid TeacherId,
    DateTime ScheduledDate,
    int DurationMinutes,
    ScheduleStatus Status
);

public static class GetAllEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanViewSchedule))
             .WithName("GetSchedules")
             .WithSummary("Get all schedules")
             .Produces<PagedResponse<ScheduleListResponse>>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> Handle(
        IScheduleRepository repository,
        CancellationToken ct,
        Guid? enrollmentId = null,
        Guid? groupId = null,
        Guid? teacherId = null,
        ScheduleStatus? status = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        int page = 1,
        int pageSize = 20
    )
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 100) pageSize = 100;

        var (schedules, totalCount) = await repository.GetAllAsync(
            enrollmentId,
            groupId,
            teacherId,
            status,
            dateFrom?.ToUniversalTime(),
            dateTo?.ToUniversalTime(),
            page,
            pageSize,
            ct
        );

        var items = schedules.Select(s => new ScheduleListResponse(
            s.Id,
            s.EnrollmentId,
            s.GroupId,
            s.TeacherId,
            s.ScheduledDate,
            s.DurationMinutes,
            s.Status)
        ).ToList();

        var response = new PagedResponse<ScheduleListResponse>(
            items,
            page,
            pageSize,
            totalCount,
            (int)Math.Ceiling(totalCount / (double)pageSize)
        );

        return Results.Ok(response);
    }
}
