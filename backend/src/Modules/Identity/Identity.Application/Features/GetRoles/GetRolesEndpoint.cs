using Identity.Application.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Identity.Application.Features.GetRoles;

public sealed record RoleResponse(Guid Id, string Name);

public static class GetRolesEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/roles", Handle)
             .RequireAuthorization()
             .WithName("GetRoles")
             .WithSummary("Get all roles")
             .Produces<IReadOnlyList<RoleResponse>>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> Handle(
        IRoleRepository roleRepository,
        CancellationToken ct
    )
    {
        var roles = await roleRepository.GetAllAsync(ct);

        var response = roles
            .Select(r => new RoleResponse(r.Id, r.Name))
            .ToList();

        return Results.Ok(response);
    }
}
