using Courses.Application.Abstractions;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace Courses.Application.Features.DeleteCourse;

public sealed record DeleteCourseRequest;

public static class DeleteEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapDelete("/{id:guid}", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanManageCourses))
             .WithName("DeleteCourse")
             .WithSummary("Delete a course")
             .Produces(StatusCodes.Status204NoContent)
             .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        Guid id,
        ICourseRepository repository,
        ICoursesUnitOfWork unitOfWork,
        ILogger<DeleteCourseRequest> logger,
        CancellationToken ct
    )
    {
        var course = await repository.GetByIdAsync(id, ct);
        if (course is null)
            return Results.Problem(
                detail: $"Course with id '{id}' not found.",
                statusCode: StatusCodes.Status404NotFound
            );

        await repository.DeleteAsync(course, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Course deleted: {CourseId}", id);

        return Results.NoContent();
    }
}
