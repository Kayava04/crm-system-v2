using Billing.Application.Abstractions;
using Billing.Contracts;

namespace Billing.Application;

internal sealed class InvoiceReminderSourceService(IStudentInvoiceRepository repository) : IInvoiceReminderSource
{
    public async Task<IReadOnlyList<RemindableInvoice>> GetUnpaidDueUntilAsync(
        DateOnly dueUntil,
        bool includeOverdue,
        CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var invoices = await repository.GetUnpaidDueUntilAsync(dueUntil, ct);

        return invoices
            .Select(i => new RemindableInvoice(i.Id, i.StudentId, i.Period, i.Amount, i.DueDate, i.DueDate < today))
            .Where(i => includeOverdue || !i.IsOverdue)
            .ToList();
    }
}
