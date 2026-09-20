using System.Security.Claims;
using Identity.Contracts.Enums;
using Materials.Application.Abstractions;
using Materials.Application.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace Materials.Application.Features.DeleteMaterial;

public sealed record DeleteMaterialRequest;

public static class DeleteEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapDelete("/{id:guid}", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanManageMaterials))
             .WithName("DeleteMaterial")
             .WithSummary("Delete a material (author or admin only)")
             .Produces(StatusCodes.Status204NoContent)
             .ProducesProblem(StatusCodes.Status403Forbidden)
             .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        Guid id,
        ClaimsPrincipal user,
        IMaterialRepository repository,
        IMaterialsUnitOfWork unitOfWork,
        ILogger<DeleteMaterialRequest> logger,
        CancellationToken ct
    )
    {
        var material = await repository.GetByIdAsync(id, ct);
        if (material is null)
            return Results.Problem(
                detail: $"Material with id '{id}' not found.",
                statusCode: StatusCodes.Status404NotFound
            );

        if (!MaterialAccess.CanModify(user, material))
        {
            logger.LogWarning("User {UserId} tried to delete foreign material {MaterialId}",
                MaterialAccess.GetUserId(user), id);

            return Results.Problem(
                detail: "Only the author or an administrator can delete this material.",
                statusCode: StatusCodes.Status403Forbidden
            );
        }

        await repository.DeleteAsync(material, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Material deleted: {MaterialId}", id);

        return Results.NoContent();
    }
}
