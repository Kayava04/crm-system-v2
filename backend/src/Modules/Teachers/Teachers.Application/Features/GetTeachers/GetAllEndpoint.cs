using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Shared.Kernel.Common;
using Teachers.Application.Abstractions;
using Teachers.Domain.Enums;

namespace Teachers.Application.Features.GetTeachers;

public sealed record TeacherListResponse(
    Guid Id,
    string FullName,
    string Email,
    string PhoneNumber,
    TeacherStatus Status
);

public static class GetAllEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanViewTeachers))
             .WithName("GetTeachers")
             .WithSummary("Get all teachers")
             .Produces<PagedResponse<TeacherListResponse>>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> Handle(
        ITeacherRepository repository,
        CancellationToken ct,
        string? search = null,
        string? city = null,
        TeacherStatus? status = null,
        int page = 1,
        int pageSize = 20
    )
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 100) pageSize = 100;

        var (teachers, totalCount) = await repository.GetAllAsync(
            search, city, status, page, pageSize, ct);

        var items = teachers.Select(t => new TeacherListResponse(
            t.Id,
            $"{t.FirstName} {t.LastName}",
            t.Email,
            t.PhoneNumber,
            t.Status)
        ).ToList();

        var response = new PagedResponse<TeacherListResponse>(
            items,
            page,
            pageSize,
            totalCount,
            (int)Math.Ceiling(totalCount / (double)pageSize)
        );

        return Results.Ok(response);
    }
}
