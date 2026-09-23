using Billing.Application.Abstractions;
using Billing.Domain.Enums;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Shared.Kernel.Common;

namespace Billing.Application.Features.GetInvoices;

public sealed record InvoiceListResponse(
    Guid Id,
    Guid EnrollmentId,
    Guid StudentId,
    string Period,
    decimal Amount,
    DateOnly DueDate,
    InvoiceStatus Status
);

public static class GetInvoicesEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanViewPayments))
             .WithName("GetInvoices")
             .WithSummary("Get all student invoices")
             .Produces<PagedResponse<InvoiceListResponse>>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> Handle(
        IStudentInvoiceRepository repository,
        CancellationToken ct,
        Guid? studentId = null,
        Guid? enrollmentId = null,
        string? period = null,
        InvoiceStatus? status = null,
        int page = 1,
        int pageSize = 20
    )
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 100) pageSize = 100;

        var (invoices, totalCount) = await repository.GetAllAsync(
            studentId, enrollmentId, period, status, page, pageSize, ct);

        var items = invoices.Select(i => new InvoiceListResponse(
            i.Id,
            i.EnrollmentId,
            i.StudentId,
            i.Period,
            i.Amount,
            i.DueDate,
            i.Status)
        ).ToList();

        var response = new PagedResponse<InvoiceListResponse>(
            items,
            page,
            pageSize,
            totalCount,
            (int)Math.Ceiling(totalCount / (double)pageSize)
        );

        return Results.Ok(response);
    }
}
