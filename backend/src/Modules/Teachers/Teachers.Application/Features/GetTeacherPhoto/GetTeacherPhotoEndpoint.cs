using Identity.Contracts;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Net.Http.Headers;
using Teachers.Application.Abstractions;

namespace Teachers.Application.Features.GetTeacherPhoto;

public static class GetTeacherPhotoEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/{id:guid}/photo", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanViewTeachers))
             .WithName("GetTeacherPhoto")
             .WithSummary("The profile photo of a teacher (the one the teacher uploaded to their account)")
             .Produces(StatusCodes.Status200OK, contentType: "image/jpeg")
             .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        Guid id,
        HttpContext http,
        ITeacherRepository repository,
        IUserPhotos photos,
        CancellationToken ct
    )
    {
        var teacher = await repository.GetByIdAsync(id, ct);
        if (teacher is null)
            return Results.Problem(
                detail: $"Teacher with id '{id}' not found.",
                statusCode: StatusCodes.Status404NotFound
            );

        if (teacher.UserId is not { } userId)
            return Results.Problem(detail: "The teacher has no account, so no photo.", statusCode: StatusCodes.Status404NotFound);

        var photo = await photos.OpenAsync(userId, ct);
        if (photo is null)
            return Results.Problem(detail: "The teacher has no photo.", statusCode: StatusCodes.Status404NotFound);

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
