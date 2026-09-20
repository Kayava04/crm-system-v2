using System.Security.Claims;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Teachers.Application.Abstractions;
using Teachers.Application.Features.GetTeacherById;

namespace Teachers.Application.Features.GetMyTeacherProfile;

public static class GetMyTeacherProfileEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/me", Handle)
             .RequireAuthorization(policy => policy.RequireRole(nameof(SystemRole.Teacher)))
             .WithName("GetMyTeacherProfile")
             .WithSummary("The profile of the current teacher (including their salary rates)")
             .Produces<TeacherDetailResponse>(StatusCodes.Status200OK)
             .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        ClaimsPrincipal user,
        ITeacherRepository repository,
        CancellationToken ct
    )
    {
        // JwtBearer maps the "sub" claim to NameIdentifier by default
        var claim = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        if (!Guid.TryParse(claim, out var userId))
            return Results.Problem(detail: "Invalid user identity.", statusCode: StatusCodes.Status401Unauthorized);

        var teacher = await repository.GetByUserIdAsync(userId, ct);

        return teacher is null
            ? Results.Problem(detail: "Teacher profile is not linked to this account.", statusCode: StatusCodes.Status404NotFound)
            : Results.Ok(TeacherDetailResponse.From(teacher));
    }
}
