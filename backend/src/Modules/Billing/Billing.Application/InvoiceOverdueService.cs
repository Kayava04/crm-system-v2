using Billing.Application.Abstractions;
using Billing.Contracts;
using Microsoft.Extensions.Logging;

namespace Billing.Application;

internal sealed class InvoiceOverdueService(
    IStudentInvoiceRepository repository,
    IBillingUnitOfWork unitOfWork,
    ILogger<InvoiceOverdueService> logger
) : IInvoiceOverdueMarker
{
    public async Task<int> MarkOverdueAsync(CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var invoices = await repository.GetPendingDueBeforeAsync(today, ct);

        foreach (var invoice in invoices)
            invoice.MarkOverdue();

        if (invoices.Count > 0)
            await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Invoices marked as overdue: {Count}", invoices.Count);

        return invoices.Count;
    }
}
