using System.Security.Claims;
using Identity.Application.Abstractions;
using Identity.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Identity.Application.Features.Me;

// The profile is the student or teacher record of this account, null for administrators
public sealed record MeProfile(string Type, Guid Id, string FullName);

public sealed record MeResponse(
    Guid UserId,
    string Email,
    bool MustChangePassword,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    MeProfile? Profile
);

public static class MeEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/me", Handle)
             .RequireAuthorization()
             .WithName("GetMe")
             .WithSummary("Who am I: account, roles, permissions and the student or teacher profile")
             .Produces<MeResponse>(StatusCodes.Status200OK)
             .ProducesProblem(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> Handle(
        ClaimsPrincipal principal,
        IUserRepository userRepository,
        IEnumerable<IProfileLinker> profileLinkers,
        CancellationToken ct
    )
    {
        // JwtBearer maps the "sub" claim to NameIdentifier by default
        var claim = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        if (!Guid.TryParse(claim, out var userId))
            return Results.Problem(detail: "Invalid user identity.", statusCode: StatusCodes.Status401Unauthorized);

        var user = await userRepository.GetByIdAsync(userId, ct);
        if (user is null || !user.IsActive)
            return Results.Problem(detail: "User not found or deactivated.", statusCode: StatusCodes.Status401Unauthorized);

        var roles = await userRepository.GetUserRolesAsync(userId, ct);
        var permissions = await userRepository.GetUserPermissionsAsync(userId, ct);

        MeProfile? profile = null;
        foreach (var linker in profileLinkers)
        {
            var found = await linker.FindByUserAsync(userId, ct);
            if (found is null)
                continue;

            profile = new MeProfile(found.Type, found.Id, found.FullName);
            break;
        }

        return Results.Ok(new MeResponse(
            user.Id,
            user.Email!,
            user.MustChangePassword,
            roles.Select(r => r.Name).Order().ToList(),
            permissions.Select(p => p.Name).Order().ToList(),
            profile));
    }
}
