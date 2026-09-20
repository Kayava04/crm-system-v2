using System.Security.Claims;
using Billing.Application.Abstractions;
using Billing.Application.Features.GetPayrolls;
using Billing.Domain.Enums;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Shared.Kernel.Common;
using Teachers.Contracts;

namespace Billing.Application.Features.GetMyPayrolls;

public static class GetMyPayrollsEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/my", Handle)
             .RequireAuthorization(policy => policy.RequireRole(nameof(SystemRole.Teacher)))
             .WithName("GetMyPayrolls")
             .WithSummary("The payrolls of the current teacher")
             .Produces<PagedResponse<PayrollListResponse>>(StatusCodes.Status200OK)
             .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        ClaimsPrincipal user,
        ITeacherPayrollRepository repository,
        ITeacherLookup teacherLookup,
        CancellationToken ct,
        string? period = null,
        PayrollStatus? status = null,
        int page = 1,
        int pageSize = 20
    )
    {
        // JwtBearer maps the "sub" claim to NameIdentifier by default
        var claim = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        if (!Guid.TryParse(claim, out var userId))
            return Results.Problem(detail: "Invalid user identity.", statusCode: StatusCodes.Status401Unauthorized);

        var teacher = await teacherLookup.GetByUserIdAsync(userId, ct);
        if (teacher is null)
            return Results.Problem(detail: "Teacher profile is not linked to this account.", statusCode: StatusCodes.Status404NotFound);

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 100) pageSize = 100;

        var (payrolls, totalCount) = await repository.GetAllAsync(teacher.Id, period, status, page, pageSize, ct);

        var items = payrolls.Select(p => new PayrollListResponse(
            p.Id, p.TeacherId, p.Period, p.CompletedLessonsCount, p.TotalAmount, p.Status)).ToList();

        return Results.Ok(new PagedResponse<PayrollListResponse>(
            items, page, pageSize, totalCount, (int)Math.Ceiling(totalCount / (double)pageSize)));
    }
}
