using Identity.Application.Abstractions;
using Identity.Domain.Entities;
using Identity.Infrastructure.Postgres.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Postgres.Repositories;

internal sealed class RoleRepository(IdentityDbContext context) : IRoleRepository
{
    public async Task<bool> AnyAsync(CancellationToken ct = default) =>
        await context.Roles.AsNoTracking().AnyAsync(ct);

    public async Task<Role?> GetByNameAsync(string name, CancellationToken ct = default) =>
        await context.Roles.FirstOrDefaultAsync(r => r.Name == name, ct);

    public async Task AddAsync(Role role, CancellationToken ct = default) =>
        await context.Roles.AddAsync(role, ct);

    public async Task AssignPermissionAsync(Guid roleId, Guid permissionId, CancellationToken ct = default)
    {
        var rolePermission = RolePermission.Create(roleId, permissionId);
        await context.RolePermissions.AddAsync(rolePermission, ct);
    }

    public async Task<IReadOnlyList<Guid>> GetRolePermissionIdsAsync(Guid roleId, CancellationToken ct = default) =>
        await context.RolePermissions
            .AsNoTracking()
            .Where(rp => rp.RoleId == roleId)
            .Select(rp => rp.PermissionId)
            .ToListAsync(ct);
}
