using System.Security.Claims;
using Billing.Application.Abstractions;
using Billing.Application.Features.GetInvoices;
using Billing.Domain.Enums;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Shared.Kernel.Common;
using Students.Contracts;

namespace Billing.Application.Features.GetMyInvoices;

public static class GetMyInvoicesEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/my", Handle)
             .RequireAuthorization(policy => policy.RequireRole(nameof(SystemRole.Student)))
             .WithName("GetMyInvoices")
             .WithSummary("The invoices of the current student")
             .Produces<PagedResponse<InvoiceListResponse>>(StatusCodes.Status200OK)
             .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        ClaimsPrincipal user,
        IStudentInvoiceRepository repository,
        IStudentLookup studentLookup,
        CancellationToken ct,
        InvoiceStatus? status = null,
        int page = 1,
        int pageSize = 20
    )
    {
        // JwtBearer maps the "sub" claim to NameIdentifier by default
        var claim = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        if (!Guid.TryParse(claim, out var userId))
            return Results.Problem(detail: "Invalid user identity.", statusCode: StatusCodes.Status401Unauthorized);

        var student = await studentLookup.GetByUserIdAsync(userId, ct);
        if (student is null)
            return Results.Problem(detail: "Student profile is not linked to this account.", statusCode: StatusCodes.Status404NotFound);

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 100) pageSize = 100;

        var (invoices, totalCount) = await repository.GetAllAsync(student.Id, null, null, status, page, pageSize, ct);

        var items = invoices.Select(i => new InvoiceListResponse(
            i.Id, i.EnrollmentId, i.StudentId, i.Period, i.Amount, i.DueDate, i.Status)).ToList();

        return Results.Ok(new PagedResponse<InvoiceListResponse>(
            items, page, pageSize, totalCount, (int)Math.Ceiling(totalCount / (double)pageSize)));
    }
}
