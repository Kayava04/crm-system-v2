using Billing.Domain.Entities;
using Billing.Domain.Enums;

namespace Crm.UnitTests.Domain;

public class BillingEntitiesTests
{
    [Theory]
    [InlineData(1000, 100, 0, 1000)]
    [InlineData(1000, 100, 8, 1800)]
    [InlineData(0, 250.5, 4, 1002)]
    public void Payroll_total_is_base_plus_rate_times_completed_lessons(decimal baseSalary, decimal rate, int lessons, decimal expected)
    {
        var payroll = TeacherPayroll.Create(Guid.NewGuid(), "2030-05", baseSalary, rate, lessons);

        Assert.Equal(expected, payroll.TotalAmount);
        Assert.Equal(PayrollStatus.Pending, payroll.Status);
        Assert.Null(payroll.PaidAt);
    }

    [Fact]
    public void Payroll_MarkPaid_sets_status_and_time()
    {
        var payroll = TeacherPayroll.Create(Guid.NewGuid(), "2030-05", 1000, 100, 1);

        payroll.MarkPaid();

        Assert.Equal(PayrollStatus.Paid, payroll.Status);
        Assert.NotNull(payroll.PaidAt);
    }

    [Fact]
    public void Invoice_starts_pending()
    {
        var invoice = StudentInvoice.Create(Guid.NewGuid(), Guid.NewGuid(), "2030-05", 2500m, new DateOnly(2030, 5, 10));

        Assert.Equal(InvoiceStatus.Pending, invoice.Status);
        Assert.Null(invoice.PaidAt);
    }

    [Fact]
    public void Invoice_can_become_overdue_and_then_paid()
    {
        var invoice = StudentInvoice.Create(Guid.NewGuid(), Guid.NewGuid(), "2030-05", 2500m, new DateOnly(2030, 5, 10));

        invoice.MarkOverdue();
        Assert.Equal(InvoiceStatus.Overdue, invoice.Status);

        invoice.MarkPaid();
        Assert.Equal(InvoiceStatus.Paid, invoice.Status);
        Assert.NotNull(invoice.PaidAt);
    }
}
