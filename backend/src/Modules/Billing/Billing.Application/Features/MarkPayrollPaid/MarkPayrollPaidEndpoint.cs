using Billing.Application.Abstractions;
using Billing.Domain.Enums;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace Billing.Application.Features.MarkPayrollPaid;

public sealed record MarkPayrollPaidRequest;

public static class MarkPayrollPaidEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPut("/{id:guid}/paid", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanManagePayments))
             .WithName("MarkPayrollPaid")
             .WithSummary("Mark a payroll as paid")
             .Produces(StatusCodes.Status204NoContent)
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> Handle(
        Guid id,
        ITeacherPayrollRepository repository,
        IBillingUnitOfWork unitOfWork,
        ILogger<MarkPayrollPaidRequest> logger,
        CancellationToken ct
    )
    {
        var payroll = await repository.GetByIdAsync(id, ct);
        if (payroll is null)
            return Results.Problem(
                detail: $"Payroll with id '{id}' not found.",
                statusCode: StatusCodes.Status404NotFound
            );

        if (payroll.Status == PayrollStatus.Paid)
            return Results.Problem(
                detail: "Payroll is already paid.",
                statusCode: StatusCodes.Status409Conflict
            );

        payroll.MarkPaid();

        await repository.UpdateAsync(payroll, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Payroll paid: {PayrollId}", id);

        return Results.NoContent();
    }
}
