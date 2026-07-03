using Education.Contracts.Enums;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Shared.Kernel.Common;
using Students.Application.Abstractions;
using Students.Domain.Enums;

namespace Students.Application.Features.GetStudents;

public sealed record StudentListResponse(
    Guid Id,
    string FullName,
    string Email,
    string PhoneNumber,
    bool IsChild
);

public static class GetAllEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/", Handle)
             .WithName("GetStudents")
             .RequireAuthorization(nameof(SystemPermission.CanViewStudents))
             .WithSummary("Get all students")
             .Produces<PagedResponse<StudentListResponse>>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> Handle(
        IStudentRepository repository,
        CancellationToken ct,
        string? search = null,
        string? city = null,
        bool? isChild = null,
        Language? language = null,
        Level? currentLevel = null,
        Format? format = null,
        int page = 1,
        int pageSize = 20
    )
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 100) pageSize = 100;

        var (students, totalCount) = await repository.GetAllAsync(
            search, city, isChild, language, currentLevel, format, page, pageSize, ct
        );

        var items = students.Select(s => new StudentListResponse(
            s.Id,
            $"{s.FirstName} {s.LastName}",
            s.Email,
            s.PhoneNumber,
            s.IsChild)
        ).ToList();

        var response = new PagedResponse<StudentListResponse>(
            items,
            page,
            pageSize,
            totalCount,
            (int)Math.Ceiling(totalCount / (double)pageSize)
        );

        return Results.Ok(response);
    }
}
