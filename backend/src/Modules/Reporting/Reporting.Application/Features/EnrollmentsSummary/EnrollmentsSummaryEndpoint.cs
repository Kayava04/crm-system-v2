using Courses.Contracts;
using Enrollments.Contracts;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Reporting.Application.Features.EnrollmentsSummary;

public sealed record CourseEnrollmentsResponse(
    Guid CourseId,
    string CourseName,
    int Count
);

public sealed record EnrollmentsSummaryResponse(
    int Total,
    IReadOnlyDictionary<string, int> ByStatus,
    IReadOnlyList<CourseEnrollmentsResponse> ByCourse
);

public static class EnrollmentsSummaryEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/enrollments/summary", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanViewReports))
             .WithName("GetEnrollmentsSummary")
             .WithSummary("Get enrollments summary (total, by status, by course)")
             .Produces<EnrollmentsSummaryResponse>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> Handle(
        IEnrollmentStatistics statistics,
        ICourseLookup courseLookup,
        CancellationToken ct
    )
    {
        var summary = await statistics.GetSummaryAsync(ct);

        var byCourse = new List<CourseEnrollmentsResponse>();
        foreach (var item in summary.ByCourse)
        {
            var course = await courseLookup.GetByIdAsync(item.CourseId, ct);

            byCourse.Add(new CourseEnrollmentsResponse(
                item.CourseId,
                course?.Name ?? "Unknown course",
                item.Count
            ));
        }

        return Results.Ok(new EnrollmentsSummaryResponse(summary.Total, summary.ByStatus, byCourse));
    }
}
