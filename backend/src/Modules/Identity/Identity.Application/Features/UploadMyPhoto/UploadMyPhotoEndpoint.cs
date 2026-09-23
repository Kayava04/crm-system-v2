using System.Security.Claims;
using Identity.Application.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Identity.Application.Features.UploadMyPhoto;

public sealed record PhotoResponse(
    bool HasPhoto,
    string? Url,
    string? ContentType,
    long? SizeBytes,
    DateTime? UploadedAt
);

public static class UploadMyPhotoEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPut("/me/photo", Handle)
             .RequireAuthorization()
             .DisableAntiforgery()
             // refused by the server before the body is read when it is far too big
             .WithMetadata(new RequestSizeLimitAttribute(UserPhotoService.MaxBytes + 1024 * 1024))
             .WithName("UploadMyPhoto")
             .WithSummary("Upload or replace my profile photo (multipart field 'file'; JPEG, PNG or WebP, up to 5 MB)")
             .Accepts<IFormFile>("multipart/form-data")
             .Produces<PhotoResponse>(StatusCodes.Status200OK)
             .ProducesValidationProblem()
             .ProducesProblem(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> Handle(
        IFormFile? file,
        ClaimsPrincipal user,
        UserPhotoService photos,
        CancellationToken ct
    )
    {
        // JwtBearer maps the "sub" claim to NameIdentifier by default
        var claim = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        if (!Guid.TryParse(claim, out var userId))
            return Results.Problem(detail: "Invalid user identity.", statusCode: StatusCodes.Status401Unauthorized);

        if (file is null || file.Length == 0)
            return Problem("No file was uploaded. Send the picture in the 'file' field of a multipart form.");

        if (file.Length > UserPhotoService.MaxBytes)
            return Problem($"The photo is larger than {UserPhotoService.MaxBytes / 1024 / 1024} MB.");

        using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, ct);
        var bytes = buffer.ToArray();

        var format = ImageFormat.Detect(bytes);
        if (format is null)
            return Problem("Only JPEG, PNG or WebP pictures are accepted.");

        // the client's file name is kept for information only; the stored name is generated
        var info = await photos.SaveAsync(userId, Path.GetFileName(file.FileName ?? "photo"), format, bytes, ct);

        return Results.Ok(new PhotoResponse(true, "/api/auth/me/photo", info.ContentType, info.SizeBytes, info.UploadedAt));
    }

    private static IResult Problem(string message) =>
        Results.ValidationProblem(new Dictionary<string, string[]> { ["file"] = [message] });
}
