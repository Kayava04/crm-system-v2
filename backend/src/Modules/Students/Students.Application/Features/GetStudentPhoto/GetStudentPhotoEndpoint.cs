using Identity.Contracts;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Net.Http.Headers;
using Students.Application.Abstractions;

namespace Students.Application.Features.GetStudentPhoto;

public static class GetStudentPhotoEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/{id:guid}/photo", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanViewStudents))
             .WithName("GetStudentPhoto")
             .WithSummary("The profile photo of a student (the one the student uploaded to their account)")
             .Produces(StatusCodes.Status200OK, contentType: "image/jpeg")
             .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        Guid id,
        HttpContext http,
        IStudentRepository repository,
        IUserPhotos photos,
        CancellationToken ct
    )
    {
        var student = await repository.GetByIdAsync(id, ct);
        if (student is null)
            return Results.Problem(
                detail: $"Student with id '{id}' not found.",
                statusCode: StatusCodes.Status404NotFound
            );

        if (student.UserId is not { } userId)
            return Results.Problem(detail: "The student has no account, so no photo.", statusCode: StatusCodes.Status404NotFound);

        var photo = await photos.OpenAsync(userId, ct);
        if (photo is null)
            return Results.Problem(detail: "The student has no photo.", statusCode: StatusCodes.Status404NotFound);

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
