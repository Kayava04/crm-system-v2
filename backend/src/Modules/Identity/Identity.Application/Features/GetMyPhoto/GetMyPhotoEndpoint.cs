using System.Security.Claims;
using Identity.Application.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Net.Http.Headers;

namespace Identity.Application.Features.GetMyPhoto;

public static class GetMyPhotoEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/me/photo", Handle)
             .RequireAuthorization()
             .WithName("GetMyPhoto")
             .WithSummary("My profile photo as an image (supports If-None-Match, so the browser can cache it)")
             .Produces(StatusCodes.Status200OK, contentType: "image/jpeg")
             .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        HttpContext http,
        ClaimsPrincipal user,
        UserPhotoService photos,
        CancellationToken ct
    )
    {
        // JwtBearer maps the "sub" claim to NameIdentifier by default
        var claim = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        if (!Guid.TryParse(claim, out var userId))
            return Results.Problem(detail: "Invalid user identity.", statusCode: StatusCodes.Status401Unauthorized);

        var photo = await photos.OpenAsync(userId, ct);
        if (photo is null)
            return Results.Problem(detail: "You have no photo.", statusCode: StatusCodes.Status404NotFound);

        // The browser must not guess a different type from the content, and must ask again before reusing a cached copy
        http.Response.Headers.XContentTypeOptions = "nosniff";
        http.Response.Headers.CacheControl = "private, no-cache";

        return Results.File(
            photo.Content,
            photo.ContentType,
            lastModified: new DateTimeOffset(DateTime.SpecifyKind(photo.UploadedAt, DateTimeKind.Utc)),
            entityTag: new EntityTagHeaderValue($"\"{photo.Version:N}\""));
    }
}
