using Billing.Application.Abstractions;
using Billing.Contracts;
using Billing.Domain.Enums;

namespace Billing.Application;

internal sealed class BillingStatisticsService(
    IStudentInvoiceRepository invoices,
    ITeacherPayrollRepository payrolls
) : IBillingStatistics
{
    public async Task<BillingSummaryResult> GetSummaryAsync(
        DateTime from,
        DateTime to,
        CancellationToken ct = default)
    {
        // Repositories share one scoped DbContext, so the queries run one after another
        var income = await invoices.SumPaidAsync(from, to, ct);
        var expenses = await payrolls.SumPaidAsync(from, to, ct);
        var pendingInvoices = await invoices.SumByStatusAsync(InvoiceStatus.Pending, ct);
        var overdueInvoices = await invoices.SumByStatusAsync(InvoiceStatus.Overdue, ct);
        var pendingPayroll = await payrolls.SumByStatusAsync(PayrollStatus.Pending, ct);

        return new BillingSummaryResult(income, expenses, pendingInvoices, overdueInvoices, pendingPayroll);
    }
}
