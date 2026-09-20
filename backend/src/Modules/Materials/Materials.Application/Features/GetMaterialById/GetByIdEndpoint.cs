using Identity.Contracts.Enums;
using Materials.Application.Abstractions;
using Materials.Application.Services;
using Materials.Domain.Entities;
using Materials.Domain.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Materials.Application.Features.GetMaterialById;

public sealed record MaterialDetailResponse(
    Guid Id,
    Guid CourseId,
    Guid AuthorUserId,
    MaterialType Type,
    string Title,
    string? Description,
    string? Body,
    string? Url,
    string? YouTubeVideoId,
    string? EmbedUrl,
    string? ThumbnailUrl,
    DateTime CreatedAt,
    DateTime? UpdatedAt
)
{
    internal static MaterialDetailResponse From(Material m) => new(
        m.Id,
        m.CourseId,
        m.AuthorUserId,
        m.Type,
        m.Title,
        m.Description,
        m.Body,
        m.Url,
        m.YouTubeVideoId,
        m.YouTubeVideoId is null ? null : YouTubeUrl.EmbedUrl(m.YouTubeVideoId),
        m.YouTubeVideoId is null ? null : YouTubeUrl.ThumbnailUrl(m.YouTubeVideoId),
        m.CreatedAt,
        m.UpdatedAt
    );
}

public static class GetByIdEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/{id:guid}", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanViewMaterials))
             .WithName("GetMaterialById")
             .WithSummary("Get material by id")
             .Produces<MaterialDetailResponse>(StatusCodes.Status200OK)
             .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        Guid id,
        IMaterialRepository repository,
        CancellationToken ct
    )
    {
        var material = await repository.GetByIdAsync(id, ct);
        if (material is null)
            return Results.Problem(
                detail: $"Material with id '{id}' not found.",
                statusCode: StatusCodes.Status404NotFound
            );

        return Results.Ok(MaterialDetailResponse.From(material));
    }
}
