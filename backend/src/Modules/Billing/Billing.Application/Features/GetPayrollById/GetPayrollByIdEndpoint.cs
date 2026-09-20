using Billing.Application.Abstractions;
using Billing.Domain.Enums;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Billing.Application.Features.GetPayrollById;

public sealed record PayrollDetailResponse(
    Guid Id,
    Guid TeacherId,
    string Period,
    decimal BaseSalary,
    decimal LessonsRate,
    int CompletedLessonsCount,
    decimal TotalAmount,
    PayrollStatus Status,
    DateTime? PaidAt,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public static class GetPayrollByIdEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/{id:guid}", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanViewPayments))
             .WithName("GetPayrollById")
             .WithSummary("Get payroll by id")
             .Produces<PayrollDetailResponse>(StatusCodes.Status200OK)
             .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        Guid id,
        ITeacherPayrollRepository repository,
        CancellationToken ct
    )
    {
        var payroll = await repository.GetByIdAsync(id, ct);
        if (payroll is null)
            return Results.Problem(
                detail: $"Payroll with id '{id}' not found.",
                statusCode: StatusCodes.Status404NotFound
            );

        var response = new PayrollDetailResponse(
            payroll.Id,
            payroll.TeacherId,
            payroll.Period,
            payroll.BaseSalary,
            payroll.LessonsRate,
            payroll.CompletedLessonsCount,
            payroll.TotalAmount,
            payroll.Status,
            payroll.PaidAt,
            payroll.CreatedAt,
            payroll.UpdatedAt
        );

        return Results.Ok(response);
    }
}
