using Identity.Contracts.Enums;

namespace Identity.Contracts;

public interface IUserDirectory
{
    Task<IReadOnlyList<Guid>> GetActiveUserIdsByRoleAsync(SystemRole role, CancellationToken ct = default);

    Task<StaffSalaryLookupResult?> GetStaffSalaryAsync(Guid userId, CancellationToken ct = default);
}

public sealed record StaffSalaryLookupResult(Guid UserId, string FullName, decimal? Salary);
