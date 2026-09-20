using Enrollments.Domain.Entities;
using Enrollments.Domain.Enums;
using Shared.Kernel.Abstractions;

namespace Enrollments.Application.Abstractions;

public interface IEnrollmentRepository : IRepository<Enrollment>
{
    Task<bool> ExistsByStudentAndCourseAsync(
        Guid studentId,
        Guid courseId,
        CancellationToken ct = default
    );

    Task<IReadOnlyDictionary<EnrollmentStatus, int>> GetCountsByStatusAsync(CancellationToken ct = default);

    Task<IReadOnlyDictionary<Guid, int>> GetCountsByCourseAsync(CancellationToken ct = default);

    Task<IReadOnlyList<Enrollment>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default);

    // Tracked, for status changes
    Task<IReadOnlyList<Enrollment>> GetActiveByStudentAsync(Guid studentId, CancellationToken ct = default);

    Task<IReadOnlyList<Enrollment>> GetAutoSuspendedByStudentAsync(Guid studentId, CancellationToken ct = default);

    Task<IReadOnlyList<Enrollment>> GetByStudentAsync(Guid studentId, CancellationToken ct = default);

    Task<IReadOnlyList<Guid>> GetIdsByStudentAsync(Guid studentId, CancellationToken ct = default);

    Task<IReadOnlyList<Guid>> GetStudentIdsByEnrollmentIdsAsync(
        IReadOnlyCollection<Guid> enrollmentIds,
        CancellationToken ct = default
    );

    Task<(IReadOnlyList<Enrollment> Enrollments, int TotalCount)> GetAllAsync(
        Guid? studentId,
        Guid? courseId,
        EnrollmentStatus? status,
        int page,
        int pageSize,
        CancellationToken ct = default
    );
}
