using System.Security.Claims;
using Identity.Contracts.Enums;
using Materials.Domain.Entities;

namespace Materials.Application.Services;

internal static class MaterialAccess
{
    public static Guid? GetUserId(ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub"), out var id)
            ? id
            : null;

    public static bool CanModify(ClaimsPrincipal user, Material material) =>
        GetUserId(user) == material.AuthorUserId
        || user.IsInRole(nameof(SystemRole.Admin))
        || user.IsInRole(nameof(SystemRole.SuperAdmin));
}
