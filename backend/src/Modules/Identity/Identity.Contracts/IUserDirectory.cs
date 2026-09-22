using Identity.Contracts.Enums;

namespace Identity.Contracts;

public interface IUserDirectory
{
    // Ids of the accounts that can currently log in and have the given role
    Task<IReadOnlyList<Guid>> GetActiveUserIdsByRoleAsync(SystemRole role, CancellationToken ct = default);

    // The account's own Salary, for payroll: null if the account does not exist, is the SuperAdmin
    // system account, or is not an administrator/manager (students and teachers are paid differently)
    Task<StaffSalaryLookupResult?> GetStaffSalaryAsync(Guid userId, CancellationToken ct = default);
}

public sealed record StaffSalaryLookupResult(Guid UserId, string FullName, decimal? Salary);
