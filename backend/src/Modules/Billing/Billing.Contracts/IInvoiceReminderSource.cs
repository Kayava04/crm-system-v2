namespace Billing.Contracts;

public interface IInvoiceReminderSource
{
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
