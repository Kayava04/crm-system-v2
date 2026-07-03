using Courses.Application.Abstractions;
using Courses.Domain.Enums;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace Courses.Application.Features.ActivateCourse;

public sealed record ActivateCourseRequest;

public static class ActivateCourseEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPut("/{id:guid}/activate", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanManageCourses))
             .WithName("ActivateCourse")
             .WithSummary("Activate a course")
             .Produces(StatusCodes.Status204NoContent)
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> Handle(
        Guid id,
        ICourseRepository repository,
        ICoursesUnitOfWork unitOfWork,
        ILogger<ActivateCourseRequest> logger,
        CancellationToken ct
    )
    {
        var course = await repository.GetByIdAsync(id, ct);
        if (course is null)
            return Results.Problem(
                detail: $"Course with id '{id}' not found.",
                statusCode: StatusCodes.Status404NotFound
            );

        if (course.Status == CourseStatus.Active)
            return Results.Problem(
                detail: "Course is already active.",
                statusCode: StatusCodes.Status409Conflict
            );

        course.Activate();

        await repository.UpdateAsync(course, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Course activated: {CourseId}", id);

        return Results.NoContent();
    }
}
