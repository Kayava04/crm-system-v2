namespace Billing.Contracts;

public interface IBillingStatistics
{
    Task<BillingSummaryResult> GetSummaryAsync(
        DateTime from,
        DateTime to,
        CancellationToken ct = default
    );
}

public sealed record BillingSummaryResult(
    decimal Income,
    decimal Expenses,
    decimal PendingInvoicesAmount,
    decimal OverdueInvoicesAmount,
    decimal PendingPayrollAmount
);
