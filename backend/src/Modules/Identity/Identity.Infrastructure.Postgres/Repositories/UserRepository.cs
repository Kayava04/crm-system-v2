using Identity.Application.Abstractions;
using Identity.Domain.Entities;
using Identity.Infrastructure.Postgres.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Postgres.Repositories;

internal sealed class UserRepository(IdentityDbContext context) : IUserRepository
{
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await context.Users
            .FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        await context.Users
            .FirstOrDefaultAsync(u => u.NormalizedEmail == email.ToUpperInvariant(), ct);

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default) =>
        await context.Users
            .AsNoTracking()
            .AnyAsync(u => u.NormalizedEmail == email.ToUpperInvariant(), ct);

    public async Task<IReadOnlyList<Role>> GetUserRolesAsync(Guid userId, CancellationToken ct = default) =>
        await context.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == userId)
            .Join(context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Permission>> GetUserPermissionsAsync(Guid userId, CancellationToken ct = default)
    {
        var rolePermissions = context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Join(context.RolePermissions, ur => ur.RoleId, rp => rp.RoleId, (ur, rp) => rp.PermissionId);

        var directPermissions = context.UserPermissions
            .Where(up => up.UserId == userId)
            .Select(up => up.PermissionId);

        var permissionIds = await rolePermissions
            .Union(directPermissions)
            .ToListAsync(ct);

        return await context.Permissions
            .AsNoTracking()
            .Where(p => permissionIds.Contains(p.Id))
            .ToListAsync(ct);
    }

    public async Task AssignRoleAsync(Guid userId, Guid roleId, CancellationToken ct = default)
    {
        var userRole = UserRole.Create(userId, roleId);
        await context.UserRoles.AddAsync(userRole, ct);
    }

    public async Task AssignPermissionAsync(Guid userId, Guid permissionId, CancellationToken ct = default)
    {
        var userPermission = UserPermission.Create(userId, permissionId);
        await context.UserPermissions.AddAsync(userPermission, ct);
    }

    public Task UpdateAsync(User user, CancellationToken ct = default)
    {
        context.Users.Update(user);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<Guid>> GetActiveUserIdsByRoleAsync(string roleName, CancellationToken ct = default) =>
        await context.UserRoles
            .AsNoTracking()
            .Join(context.Roles.Where(r => r.Name == roleName), ur => ur.RoleId, r => r.Id, (ur, r) => ur.UserId)
            .Join(context.Users.Where(u => u.IsActive), id => id, u => u.Id, (id, u) => u.Id)
            .ToListAsync(ct);
}
