using Identity.Application.Abstractions;
using Identity.Contracts;
using Identity.Contracts.Enums;

namespace Identity.Application;

internal sealed class UserDirectoryService(IUserRepository repository) : IUserDirectory
{
    public async Task<IReadOnlyList<Guid>> GetActiveUserIdsByRoleAsync(SystemRole role, CancellationToken ct = default) =>
        await repository.GetActiveUserIdsByRoleAsync(role.ToString(), ct);

    public async Task<StaffSalaryLookupResult?> GetStaffSalaryAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await repository.GetByIdAsync(userId, ct);
        if (user is null)
            return null;

        var roles = await repository.GetUserRolesAsync(userId, ct);
        if (roles.Any(r => r.Name == nameof(SystemRole.SuperAdmin)) || roles.All(r => r.Name != nameof(SystemRole.Admin)))
            return null;

        // Same "Last First Middle" name a Register call would compose from these same fields
        var fullName = string.Join(' ', new[] { user.FirstName, user.LastName, user.MiddleName }
            .Where(n => !string.IsNullOrWhiteSpace(n)));

        return new StaffSalaryLookupResult(user.Id, fullName.Length == 0 ? user.Email ?? "" : fullName, user.Salary);
    }
}
