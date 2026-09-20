using Identity.Contracts.Enums;

namespace Identity.Contracts;

public interface IUserDirectory
{
    // Ids of the accounts that can currently log in and have the given role
    Task<IReadOnlyList<Guid>> GetActiveUserIdsByRoleAsync(SystemRole role, CancellationToken ct = default);
}
