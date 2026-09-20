using Scheduling.Domain.Entities;
using Shared.Kernel.Abstractions;

namespace Scheduling.Application.Abstractions;

public interface IStudyGroupRepository : IRepository<StudyGroup>
{
    Task<(IReadOnlyList<StudyGroup> Groups, int TotalCount)> GetAllAsync(
        Guid? courseId,
        Guid? teacherId,
        int page,
        int pageSize,
        CancellationToken ct = default
    );

    Task<IReadOnlyList<StudyGroup>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken ct = default
    );

    Task<IReadOnlyList<Guid>> GetGroupIdsByEnrollmentsAsync(
        IReadOnlyCollection<Guid> enrollmentIds,
        CancellationToken ct = default
    );

    // An enrollment can be in only one group of its course
    Task<bool> IsInGroupOfCourseAsync(
        Guid enrollmentId,
        Guid courseId,
        CancellationToken ct = default
    );

    Task AddMemberAsync(StudyGroupMember member, CancellationToken ct = default);

    Task RemoveMemberAsync(StudyGroupMember member, CancellationToken ct = default);
}
