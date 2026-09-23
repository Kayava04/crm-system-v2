namespace Billing.Contracts;

public interface IInvoiceOverdueMarker
{
    // Marks every pending invoice whose due date has passed as overdue; returns how many were marked
    Task<int> MarkOverdueAsync(CancellationToken ct = default);
}
