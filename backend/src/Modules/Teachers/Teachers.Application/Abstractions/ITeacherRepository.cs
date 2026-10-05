using Shared.Kernel.Abstractions;
using Teachers.Domain.Entities;
using Teachers.Domain.Enums;

namespace Teachers.Application.Abstractions;

public interface ITeacherRepository : IRepository<Teacher>
{
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);
    Task<Teacher?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<Teacher>> GetAllWithSalaryRatesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Teacher>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default);
    Task<bool> IsAvailableByIdAsync(Guid id, CancellationToken ct = default);
    Task<HashSet<string>> GetExistingEmailsAsync(IReadOnlyCollection<string> emails, CancellationToken ct = default);
    Task<IReadOnlyList<Teacher>> GetByIdsForUpdateAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default);
    Task<IReadOnlyList<Teacher>> GetForExportAsync(string? search, string? city, TeacherStatus? status, int limit, CancellationToken ct = default);
    Task<bool> ExistsByIdAsync(Guid id, CancellationToken ct = default);
    Task<(IReadOnlyList<Teacher> Teachers, int TotalCount)> GetAllAsync(
        string? search,
        string? city,
        TeacherStatus? status,
        int page,
        int pageSize,
        CancellationToken ct = default
    );
}
