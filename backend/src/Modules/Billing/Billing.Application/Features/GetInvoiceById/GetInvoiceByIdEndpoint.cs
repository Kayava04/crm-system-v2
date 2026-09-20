using Billing.Application.Abstractions;
using Billing.Domain.Enums;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Billing.Application.Features.GetInvoiceById;

public sealed record InvoiceDetailResponse(
    Guid Id,
    Guid EnrollmentId,
    Guid StudentId,
    string Period,
    decimal Amount,
    DateOnly DueDate,
    DateTime? PaidAt,
    InvoiceStatus Status,
    string? Notes,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public static class GetInvoiceByIdEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/{id:guid}", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanViewPayments))
             .WithName("GetInvoiceById")
             .WithSummary("Get invoice by id")
             .Produces<InvoiceDetailResponse>(StatusCodes.Status200OK)
             .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        Guid id,
        IStudentInvoiceRepository repository,
        CancellationToken ct
    )
    {
        var invoice = await repository.GetByIdAsync(id, ct);
        if (invoice is null)
            return Results.Problem(
                detail: $"Invoice with id '{id}' not found.",
                statusCode: StatusCodes.Status404NotFound
            );

        var response = new InvoiceDetailResponse(
            invoice.Id,
            invoice.EnrollmentId,
            invoice.StudentId,
            invoice.Period,
            invoice.Amount,
            invoice.DueDate,
            invoice.PaidAt,
            invoice.Status,
            invoice.Notes,
            invoice.CreatedAt,
            invoice.UpdatedAt
        );

        return Results.Ok(response);
    }
}
