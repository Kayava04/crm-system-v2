using Identity.Contracts.Enums;
using Materials.Application.Abstractions;
using Materials.Application.Services;
using Materials.Domain.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Shared.Kernel.Common;

namespace Materials.Application.Features.GetMaterials;

public sealed record MaterialListResponse(
    Guid Id,
    Guid CourseId,
    MaterialType Type,
    string Title,
    string? Description,
    string? ThumbnailUrl,
    DateTime CreatedAt
);

public static class GetAllEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanViewMaterials))
             .WithName("GetMaterials")
             .WithSummary("Get all materials")
             .Produces<PagedResponse<MaterialListResponse>>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> Handle(
        IMaterialRepository repository,
        CancellationToken ct,
        Guid? courseId = null,
        MaterialType? type = null,
        string? search = null,
        int page = 1,
        int pageSize = 20
    )
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 100) pageSize = 100;

        var (materials, totalCount) = await repository.GetAllAsync(courseId, type, search, page, pageSize, ct);

        var items = materials.Select(m => new MaterialListResponse(
            m.Id,
            m.CourseId,
            m.Type,
            m.Title,
            m.Description,
            m.YouTubeVideoId is null ? null : YouTubeUrl.ThumbnailUrl(m.YouTubeVideoId),
            m.CreatedAt)
        ).ToList();

        var response = new PagedResponse<MaterialListResponse>(
            items,
            page,
            pageSize,
            totalCount,
            (int)Math.Ceiling(totalCount / (double)pageSize)
        );

        return Results.Ok(response);
    }
}
