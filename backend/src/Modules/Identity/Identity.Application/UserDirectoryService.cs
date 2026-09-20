using Identity.Application.Abstractions;
using Identity.Contracts;
using Identity.Contracts.Enums;

namespace Identity.Application;

internal sealed class UserDirectoryService(IUserRepository repository) : IUserDirectory
{
    public async Task<IReadOnlyList<Guid>> GetActiveUserIdsByRoleAsync(SystemRole role, CancellationToken ct = default) =>
        await repository.GetActiveUserIdsByRoleAsync(role.ToString(), ct);
}
