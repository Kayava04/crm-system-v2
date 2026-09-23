using Billing.Contracts;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Reporting.Application.Features.BillingSummary;

public sealed record BillingSummaryResponse(
    DateOnly DateFrom,
    DateOnly DateTo,
    decimal Income,
    decimal Expenses,
    decimal NetResult,
    decimal PendingInvoicesAmount,
    decimal OverdueInvoicesAmount,
    decimal PendingPayrollAmount
);

public static class BillingSummaryEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/billing/summary", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanViewReports))
             .WithName("GetBillingSummary")
             .WithSummary("Get billing summary for a period (defaults to the current month)")
             .Produces<BillingSummaryResponse>(StatusCodes.Status200OK)
             .ProducesValidationProblem();
    }

    private static async Task<IResult> Handle(
        IBillingStatistics statistics,
        CancellationToken ct,
        DateOnly? dateFrom = null,
        DateOnly? dateTo = null
    )
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var from = dateFrom ?? new DateOnly(today.Year, today.Month, 1);
        var to = dateTo ?? today;

        if (from > to)
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["dateFrom"] = ["Date from must not be later than date to."]
            });

        // Both bounds are inclusive days, so the upper bound is the start of the next day
        var fromUtc = DateTime.SpecifyKind(from.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var toUtc = DateTime.SpecifyKind(to.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

        var summary = await statistics.GetSummaryAsync(fromUtc, toUtc, ct);

        return Results.Ok(new BillingSummaryResponse(
            from,
            to,
            summary.Income,
            summary.Expenses,
            summary.Income - summary.Expenses,
            summary.PendingInvoicesAmount,
            summary.OverdueInvoicesAmount,
            summary.PendingPayrollAmount
        ));
    }
}
