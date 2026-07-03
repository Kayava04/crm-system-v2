using Courses.Application.Abstractions;
using Courses.Domain.Enums;
using Education.Contracts.Enums;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace Courses.Application.Features.GetCourseById;

public sealed record CourseDetailResponse(
    Guid Id,
    string Name,
    Language Language,
    Level Level,
    Format Format,
    LessonType LessonType,
    int DurationMonths,
    int LessonsCount,
    int LessonsPerWeek,
    decimal Price,
    string? Description,
    CourseStatus Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public static class GetByIdEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/{id:guid}", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanViewCourses))
             .WithName("GetCourseById")
             .WithSummary("Get course by id")
             .Produces<CourseDetailResponse>(StatusCodes.Status200OK)
             .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        Guid id,
        ICourseRepository repository,
        ILogger<CourseDetailResponse> logger,
        CancellationToken ct
    )
    {
        var course = await repository.GetByIdAsync(id, ct);
        if (course is null)
            return Results.Problem(
                detail: $"Course with id '{id}' not found.",
                statusCode: StatusCodes.Status404NotFound
            );

        var response = new CourseDetailResponse(
            course.Id,
            course.Name,
            course.Language,
            course.Level,
            course.Format,
            course.LessonType,
            course.DurationMonths,
            course.LessonsCount,
            course.LessonsPerWeek,
            course.Price,
            course.Description,
            course.Status,
            course.CreatedAt,
            course.UpdatedAt
        );

        return Results.Ok(response);
    }
}
