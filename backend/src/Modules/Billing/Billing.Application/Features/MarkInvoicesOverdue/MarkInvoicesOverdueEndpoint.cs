using Billing.Contracts;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

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
        IInvoiceOverdueMarker marker,
        CancellationToken ct
    )
    {
        var count = await marker.MarkOverdueAsync(ct);

        return Results.Ok(new MarkInvoicesOverdueResponse(count));
    }
}
