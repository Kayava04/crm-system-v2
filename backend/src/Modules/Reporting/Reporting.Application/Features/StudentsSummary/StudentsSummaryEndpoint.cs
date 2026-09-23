using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Students.Contracts;

namespace Reporting.Application.Features.StudentsSummary;

public sealed record StudentsSummaryResponse(
    int Total,
    IReadOnlyDictionary<string, int> ByStatus
);

public static class StudentsSummaryEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/students/summary", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanViewReports))
             .WithName("GetStudentsSummary")
             .WithSummary("Get students summary (total and by status)")
             .Produces<StudentsSummaryResponse>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> Handle(
        IStudentStatistics statistics,
        CancellationToken ct
    )
    {
        var summary = await statistics.GetSummaryAsync(ct);

        return Results.Ok(new StudentsSummaryResponse(summary.Total, summary.ByStatus));
    }
}
