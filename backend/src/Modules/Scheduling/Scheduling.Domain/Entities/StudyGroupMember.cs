using Shared.Kernel.Primitives;

namespace Scheduling.Domain.Entities;

public sealed class StudyGroupMember : Entity
{
    public Guid GroupId { get; private set; }
    public Guid EnrollmentId { get; private set; }
    public DateTime JoinedAt { get; private set; }

    private StudyGroupMember() { }

    public static StudyGroupMember Create(Guid groupId, Guid enrollmentId)
    {
        return new StudyGroupMember
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            EnrollmentId = enrollmentId,
            JoinedAt = DateTime.UtcNow
        };
    }
}
