using System.Security.Claims;
using Identity.Application.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Identity.Application.Features.DeleteMyPhoto;

public static class DeleteMyPhotoEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapDelete("/me/photo", Handle)
             .RequireAuthorization()
             .WithName("DeleteMyPhoto")
             .WithSummary("Remove my profile photo")
             .Produces(StatusCodes.Status204NoContent)
             .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        ClaimsPrincipal user,
        UserPhotoService photos,
        CancellationToken ct
    )
    {
        // JwtBearer maps the "sub" claim to NameIdentifier by default
        var claim = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        if (!Guid.TryParse(claim, out var userId))
            return Results.Problem(detail: "Invalid user identity.", statusCode: StatusCodes.Status401Unauthorized);

        return await photos.DeleteAsync(userId, ct)
            ? Results.NoContent()
            : Results.Problem(detail: "You have no photo.", statusCode: StatusCodes.Status404NotFound);
    }
}
