using Identity.Application.Abstractions;
using Identity.Domain.Entities;
using Identity.Infrastructure.Postgres.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Postgres.Repositories;

internal sealed class PermissionRepository(IdentityDbContext context) : IPermissionRepository
{
    public async Task<bool> AnyAsync(CancellationToken ct = default) =>
        await context.Permissions.AsNoTracking().AnyAsync(ct);

    public async Task<Permission?> GetByNameAsync(string name, CancellationToken ct = default) =>
        await context.Permissions.FirstOrDefaultAsync(p => p.Name == name, ct);

    public async Task AddAsync(Permission permission, CancellationToken ct = default) =>
        await context.Permissions.AddAsync(permission, ct);
}
