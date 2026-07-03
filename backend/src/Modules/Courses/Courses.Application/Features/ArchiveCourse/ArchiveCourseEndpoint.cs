using Courses.Application.Abstractions;
using Courses.Domain.Enums;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace Courses.Application.Features.ArchiveCourse;

public sealed record ArchiveCourseRequest;

public static class ArchiveCourseEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPut("/{id:guid}/archive", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanManageCourses))
             .WithName("ArchiveCourse")
             .WithSummary("Archive a course")
             .Produces(StatusCodes.Status204NoContent)
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> Handle(
        Guid id,
        ICourseRepository repository,
        ICoursesUnitOfWork unitOfWork,
        ILogger<ArchiveCourseRequest> logger,
        CancellationToken ct
    )
    {
        var course = await repository.GetByIdAsync(id, ct);
        if (course is null)
            return Results.Problem(
                detail: $"Course with id '{id}' not found.",
                statusCode: StatusCodes.Status404NotFound
            );

        if (course.Status == CourseStatus.Archived)
            return Results.Problem(
                detail: "Course is already archived.",
                statusCode: StatusCodes.Status409Conflict
            );

        course.Archive();

        await repository.UpdateAsync(course, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Course archived: {CourseId}", id);

        return Results.NoContent();
    }
}
