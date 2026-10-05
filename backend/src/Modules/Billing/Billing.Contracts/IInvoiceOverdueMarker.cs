namespace Billing.Contracts;

public interface IInvoiceOverdueMarker
{
    Task<int> MarkOverdueAsync(CancellationToken ct = default);
}
