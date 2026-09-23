using Billing.Application.Abstractions;
using Billing.Domain.Enums;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Shared.Kernel.Common;

namespace Billing.Application.Features.GetPayrolls;

public sealed record PayrollListResponse(
    Guid Id,
    Guid? TeacherId,
    Guid? UserId,
    string Period,
    int CompletedLessonsCount,
    decimal TotalAmount,
    PayrollStatus Status
);

public static class GetPayrollsEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanViewPayments))
             .WithName("GetPayrolls")
             .WithSummary("Get all teacher payrolls")
             .Produces<PagedResponse<PayrollListResponse>>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> Handle(
        ITeacherPayrollRepository repository,
        CancellationToken ct,
        Guid? teacherId = null,
        Guid? userId = null,
        string? period = null,
        PayrollStatus? status = null,
        int page = 1,
        int pageSize = 20
    )
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 100) pageSize = 100;

        var (payrolls, totalCount) = await repository.GetAllAsync(
            teacherId, userId, period, status, page, pageSize, ct);

        var items = payrolls.Select(p => new PayrollListResponse(
            p.Id,
            p.TeacherId,
            p.UserId,
            p.Period,
            p.CompletedLessonsCount,
            p.TotalAmount,
            p.Status)
        ).ToList();

        var response = new PagedResponse<PayrollListResponse>(
            items,
            page,
            pageSize,
            totalCount,
            (int)Math.Ceiling(totalCount / (double)pageSize)
        );

        return Results.Ok(response);
    }
}
