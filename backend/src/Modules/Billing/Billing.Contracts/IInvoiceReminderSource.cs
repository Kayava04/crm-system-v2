namespace Billing.Contracts;

public interface IInvoiceReminderSource
{
    // Unpaid invoices due on or before the given date; overdue ones only when asked for
    Task<IReadOnlyList<RemindableInvoice>> GetUnpaidDueUntilAsync(
        DateOnly dueUntil,
        bool includeOverdue,
        CancellationToken ct = default
    );
}

public sealed record RemindableInvoice(
    Guid InvoiceId,
    Guid StudentId,
    string Period,
    decimal Amount,
    DateOnly DueDate,
    bool IsOverdue
);
