using Courses.Domain.Entities;
using Courses.Domain.Enums;
using Education.Contracts.Enums;
using Shared.Kernel.Abstractions;

namespace Courses.Application.Abstractions;

public interface ICourseRepository : IRepository<Course>
{
    Task<IReadOnlyList<Course>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default);
    Task<(IReadOnlyList<Course> Courses, int TotalCount)> GetAllAsync(
        string? search,
        Language? language,
        Level? level,
        Format? format,
        LessonType? lessonType,
        CourseStatus? status,
        int page,
        int pageSize,
        CancellationToken ct = default
    );
}
