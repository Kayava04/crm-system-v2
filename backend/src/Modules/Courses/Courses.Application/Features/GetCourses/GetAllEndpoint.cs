using Courses.Application.Abstractions;
using Courses.Domain.Enums;
using Education.Contracts.Enums;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Shared.Kernel.Common;

namespace Courses.Application.Features.GetCourses;

public sealed record CourseListResponse(
    Guid Id,
    string Name,
    Language Language,
    Level Level,
    Format Format,
    LessonType LessonType,
    decimal Price,
    CourseStatus Status
);

public static class GetAllEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanViewCourses))
             .WithName("GetCourses")
             .WithSummary("Get all courses")
             .Produces<PagedResponse<CourseListResponse>>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> Handle(
        ICourseRepository repository,
        CancellationToken ct,
        string? search = null,
        Language? language = null,
        Level? level = null,
        Format? format = null,
        LessonType? lessonType = null,
        CourseStatus? status = null,
        int page = 1,
        int pageSize = 20
    )
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 100) pageSize = 100;

        var (courses, totalCount) = await repository.GetAllAsync(
            search,
            language,
            level,
            format,
            lessonType,
            status,
            page,
            pageSize,
            ct
        );

        var items = courses.Select(c => new CourseListResponse(
            c.Id,
            c.Name,
            c.Language,
            c.Level,
            c.Format,
            c.LessonType,
            c.Price,
            c.Status)
        ).ToList();

        var response = new PagedResponse<CourseListResponse>(
            items,
            page,
            pageSize,
            totalCount,
            (int)Math.Ceiling(totalCount / (double)pageSize)
        );

        return Results.Ok(response);
    }
}
