using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Teachers.Contracts;

namespace Reporting.Application.Features.TeachersSummary;

public sealed record TeachersSummaryResponse(
    int Total,
    IReadOnlyDictionary<string, int> ByStatus,
    TeacherSalaryOverview SalaryOverview
);

public static class TeachersSummaryEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/teachers/summary", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanViewReports))
             .WithName("GetTeachersSummary")
             .WithSummary("Get teachers summary (total, by status, salary overview)")
             .Produces<TeachersSummaryResponse>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> Handle(
        ITeacherStatistics statistics,
        CancellationToken ct
    )
    {
        var summary = await statistics.GetSummaryAsync(ct);

        return Results.Ok(new TeachersSummaryResponse(
            summary.Total,
            summary.ByStatus,
            summary.SalaryOverview
        ));
    }
}
