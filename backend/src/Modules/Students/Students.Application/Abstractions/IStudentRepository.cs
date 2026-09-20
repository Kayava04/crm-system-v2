using Education.Contracts.Enums;
using Shared.Kernel.Abstractions;
using Students.Domain.Entities;
using Students.Domain.Enums;

namespace Students.Application.Abstractions;

public interface IStudentRepository : IRepository<Student>
{
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);
    Task<(IReadOnlyList<Student> Students, int TotalCount)> GetAllAsync(
        string? search,
        string? city,
        bool? isChild,
        Language? language,
        Level? currentLevel,
        Format? format,
        int page,
        int pageSize,
        CancellationToken ct = default
    );
    Task<IReadOnlyDictionary<StudentStatus, int>> GetCountsByStatusAsync(CancellationToken ct = default);
    Task<Student?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<bool> IsActiveByIdAsync(Guid id, CancellationToken ct = default);
    // Lower-cased emails from the given list that already belong to a student
    Task<HashSet<string>> GetExistingEmailsAsync(IReadOnlyCollection<string> emails, CancellationToken ct = default);
    // Tracked, so they can be deleted
    Task<IReadOnlyList<Student>> GetByIdsForUpdateAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default);
    // With everything an export needs (preferences, languages); at most limit rows, ordered by name
    Task<IReadOnlyList<Student>> GetForExportAsync(
        string? search,
        string? city,
        bool? isChild,
        Language? language,
        Level? currentLevel,
        Format? format,
        int limit,
        CancellationToken ct = default
    );
    Task<bool> ExistsByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Student>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default);
}
