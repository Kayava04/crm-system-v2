using System.Security.Claims;
using Identity.Contracts.Enums;
using Materials.Domain.Entities;

namespace Materials.Application.Services;

internal static class MaterialAccess
{
    // JwtBearer maps the "sub" claim to NameIdentifier by default
    public static Guid? GetUserId(ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub"), out var id)
            ? id
            : null;

    // Only the author or an administrator may change or delete a material
    public static bool CanModify(ClaimsPrincipal user, Material material) =>
        GetUserId(user) == material.AuthorUserId
        || user.IsInRole(nameof(SystemRole.Admin))
        || user.IsInRole(nameof(SystemRole.SuperAdmin));
}
