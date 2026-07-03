using Identity.Application.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Identity.Application.Features.GetPermissions;

public sealed record PermissionResponse(Guid Id, string Name);

public static class GetPermissionsEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/permissions", Handle)
             .RequireAuthorization()
             .WithName("GetPermissions")
             .WithSummary("Get all permissions")
             .Produces<IReadOnlyList<PermissionResponse>>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> Handle(
        IPermissionRepository permissionRepository,
        CancellationToken ct
    )
    {
        var permissions = await permissionRepository.GetAllAsync(ct);

        var response = permissions
            .Select(p => new PermissionResponse(p.Id, p.Name))
            .ToList();

        return Results.Ok(response);
    }
}
