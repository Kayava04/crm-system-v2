using System.Security.Claims;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Students.Application.Abstractions;
using Students.Application.Features.GetStudentById;

namespace Students.Application.Features.GetMyStudentProfile;

public static class GetMyStudentProfileEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/me", Handle)
             .RequireAuthorization(policy => policy.RequireRole(nameof(SystemRole.Student)))
             .WithName("GetMyStudentProfile")
             .WithSummary("The profile of the current student")
             .Produces<StudentDetailResponse>(StatusCodes.Status200OK)
             .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        ClaimsPrincipal user,
        IStudentRepository repository,
        CancellationToken ct
    )
    {
        // JwtBearer maps the "sub" claim to NameIdentifier by default
        var claim = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        if (!Guid.TryParse(claim, out var userId))
            return Results.Problem(detail: "Invalid user identity.", statusCode: StatusCodes.Status401Unauthorized);

        var student = await repository.GetByUserIdAsync(userId, ct);
        var response = student is null ? null : StudentDetailResponse.From(student);

        return response is null
            ? Results.Problem(detail: "Student profile is not linked to this account.", statusCode: StatusCodes.Status404NotFound)
            : Results.Ok(response);
    }
}
