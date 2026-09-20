using Billing.Application.Abstractions;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace Billing.Application.Features.MarkInvoicesOverdue;

public sealed record MarkInvoicesOverdueRequest;

public sealed record MarkInvoicesOverdueResponse(int UpdatedCount);

public static class MarkInvoicesOverdueEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPut("/mark-overdue", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanManagePayments))
             .WithName("MarkInvoicesOverdue")
             .WithSummary("Mark all pending invoices past their due date as overdue")
             .Produces<MarkInvoicesOverdueResponse>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> Handle(
        IStudentInvoiceRepository repository,
        IBillingUnitOfWork unitOfWork,
        ILogger<MarkInvoicesOverdueRequest> logger,
        CancellationToken ct
    )
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var invoices = await repository.GetPendingDueBeforeAsync(today, ct);

        foreach (var invoice in invoices)
            invoice.MarkOverdue();

        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Invoices marked as overdue: {Count}", invoices.Count);

        return Results.Ok(new MarkInvoicesOverdueResponse(invoices.Count));
    }
}
