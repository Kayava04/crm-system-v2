using System.Security.Claims;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Students.Contracts;
using Teachers.Application.Abstractions;

namespace Teachers.Application.Features.GetMyStudents;

public sealed record MyStudentResponse(
    Guid Id,
    string FullName,
    string Email,
    string PhoneNumber
);

public static class GetMyStudentsEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/me/students", Handle)
             .RequireAuthorization(policy => policy.RequireRole(nameof(SystemRole.Teacher)))
             .WithName("GetMyStudents")
             .WithSummary("Get students of the current teacher")
             .Produces<IReadOnlyList<MyStudentResponse>>(StatusCodes.Status200OK)
             .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        ClaimsPrincipal user,
        ITeacherRepository repository,
        IStudentLookup studentLookup,
        CancellationToken ct
    )
    {
        // JwtBearer maps the "sub" claim to NameIdentifier by default
        var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub");

        if (!Guid.TryParse(userIdClaim, out var userId))
            return Results.Problem(
                detail: "User identity could not be determined.",
                statusCode: StatusCodes.Status401Unauthorized
            );

        var teacher = await repository.GetByUserIdAsync(userId, ct);
        if (teacher is null)
            return Results.Problem(
                detail: "Teacher profile is not linked to this account.",
                statusCode: StatusCodes.Status404NotFound
            );

        var students = await studentLookup.GetByTeacherAsync(teacher.Id, ct);

        var response = students
            .Select(s => new MyStudentResponse(s.Id, s.FullName, s.Email, s.PhoneNumber))
            .ToList();

        return Results.Ok(response);
    }
}
