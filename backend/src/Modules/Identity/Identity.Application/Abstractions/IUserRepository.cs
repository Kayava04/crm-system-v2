using Identity.Domain.Entities;

namespace Identity.Application.Abstractions;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);
    Task<IReadOnlyList<Role>> GetUserRolesAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<Permission>> GetUserPermissionsAsync(Guid userId, CancellationToken ct = default);
    Task AssignRoleAsync(Guid userId, Guid roleId, CancellationToken ct = default);
    Task AssignPermissionAsync(Guid userId, Guid permissionId, CancellationToken ct = default);
    Task UpdateAsync(User user, CancellationToken ct = default);
    Task<IReadOnlyList<Guid>> GetActiveUserIdsByRoleAsync(string roleName, CancellationToken ct = default);
}
