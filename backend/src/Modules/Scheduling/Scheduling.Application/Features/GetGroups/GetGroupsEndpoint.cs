using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Scheduling.Application.Abstractions;
using Shared.Kernel.Common;

namespace Scheduling.Application.Features.GetGroups;

public sealed record GroupListResponse(
    Guid Id,
    Guid CourseId,
    Guid TeacherId,
    string Name,
    int MembersCount
);

public static class GetGroupsEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanViewSchedule))
             .WithName("GetStudyGroups")
             .WithSummary("Get all study groups")
             .Produces<PagedResponse<GroupListResponse>>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> Handle(
        IStudyGroupRepository repository,
        CancellationToken ct,
        Guid? courseId = null,
        Guid? teacherId = null,
        int page = 1,
        int pageSize = 20
    )
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 100) pageSize = 100;

        var (groups, totalCount) = await repository.GetAllAsync(courseId, teacherId, page, pageSize, ct);

        var items = groups.Select(g => new GroupListResponse(
            g.Id, g.CourseId, g.TeacherId, g.Name, g.Members.Count)
        ).ToList();

        var response = new PagedResponse<GroupListResponse>(
            items,
            page,
            pageSize,
            totalCount,
            (int)Math.Ceiling(totalCount / (double)pageSize)
        );

        return Results.Ok(response);
    }
}
