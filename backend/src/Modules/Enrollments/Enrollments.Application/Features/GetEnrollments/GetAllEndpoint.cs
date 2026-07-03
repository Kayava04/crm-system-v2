using Enrollments.Application.Abstractions;
using Enrollments.Domain.Enums;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Shared.Kernel.Common;

namespace Enrollments.Application.Features.GetEnrollments;

public sealed record EnrollmentListResponse(
    Guid Id,
    string EnrollmentNumber,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal EffectivePrice,
    EnrollmentStatus Status
);

public static class GetAllEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanViewEnrollments))
             .WithName("GetEnrollments")
             .WithSummary("Get all enrollments")
             .Produces<PagedResponse<EnrollmentListResponse>>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> Handle(
        IEnrollmentRepository repository,
        CancellationToken ct,
        Guid? studentId = null,
        Guid? courseId = null,
        EnrollmentStatus? status = null,
        int page = 1,
        int pageSize = 20
    )
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 100) pageSize = 100;

        var (enrollments, totalCount) = await repository.GetAllAsync(
            studentId,
            courseId,
            status,
            page,
            pageSize,
            ct
        );

        var items = enrollments.Select(e => new EnrollmentListResponse(
            e.Id,
            e.EnrollmentNumber,
            e.StartDate,
            e.EndDate,
            e.EffectivePrice,
            e.Status)
        ).ToList();

        var response = new PagedResponse<EnrollmentListResponse>(
            items,
            page,
            pageSize,
            totalCount,
            (int)Math.Ceiling(totalCount / (double)pageSize)
        );

        return Results.Ok(response);
    }
}
