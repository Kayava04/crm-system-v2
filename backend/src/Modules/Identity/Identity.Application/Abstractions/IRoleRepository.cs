using Identity.Domain.Entities;

namespace Identity.Application.Abstractions;

public interface IRoleRepository
{
    Task<bool> AnyAsync(CancellationToken ct = default);
    Task<Role?> GetByNameAsync(string name, CancellationToken ct = default);
    Task AddAsync(Role role, CancellationToken ct = default);
    Task AssignPermissionAsync(Guid roleId, Guid permissionId, CancellationToken ct = default);
    Task<IReadOnlyList<Guid>> GetRolePermissionIdsAsync(Guid roleId, CancellationToken ct = default);
    Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken ct = default);
}
