using Billing.Application.Abstractions;
using Billing.Domain.Enums;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace Billing.Application.Features.MarkInvoicePaid;

public sealed record MarkInvoicePaidRequest;

public static class MarkInvoicePaidEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPut("/{id:guid}/paid", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanManagePayments))
             .WithName("MarkInvoicePaid")
             .WithSummary("Mark an invoice as paid")
             .Produces(StatusCodes.Status204NoContent)
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> Handle(
        Guid id,
        IStudentInvoiceRepository repository,
        IBillingUnitOfWork unitOfWork,
        ILogger<MarkInvoicePaidRequest> logger,
        CancellationToken ct
    )
    {
        var invoice = await repository.GetByIdAsync(id, ct);
        if (invoice is null)
            return Results.Problem(
                detail: $"Invoice with id '{id}' not found.",
                statusCode: StatusCodes.Status404NotFound
            );

        if (invoice.Status == InvoiceStatus.Paid)
            return Results.Problem(
                detail: "Invoice is already paid.",
                statusCode: StatusCodes.Status409Conflict
            );

        invoice.MarkPaid();

        await repository.UpdateAsync(invoice, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Invoice paid: {InvoiceId}", id);

        return Results.NoContent();
    }
}
