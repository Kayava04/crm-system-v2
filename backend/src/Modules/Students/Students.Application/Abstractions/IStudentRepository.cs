using Education.Contracts.Enums;
using Shared.Kernel.Abstractions;
using Students.Domain.Entities;

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
}
