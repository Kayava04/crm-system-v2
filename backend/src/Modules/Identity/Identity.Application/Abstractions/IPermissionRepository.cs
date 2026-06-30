using Identity.Domain.Entities;

namespace Identity.Application.Abstractions;

public interface IPermissionRepository
{
    Task<bool> AnyAsync(CancellationToken ct = default);
    Task<Permission?> GetByNameAsync(string name, CancellationToken ct = default);
    Task AddAsync(Permission permission, CancellationToken ct = default);
}
