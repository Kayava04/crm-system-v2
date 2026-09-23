using Shared.Kernel.Primitives;

namespace Scheduling.Domain.Entities;

public sealed class StudyGroup : AuditableEntity
{
    public Guid CourseId { get; private set; }
    public Guid TeacherId { get; private set; }
    public string Name { get; private set; } = string.Empty;

    public IReadOnlyCollection<StudyGroupMember> Members => _members.AsReadOnly();
    private readonly List<StudyGroupMember> _members = [];

    private StudyGroup() { }

    public static StudyGroup Create(Guid courseId, Guid teacherId, string name)
    {
        return new StudyGroup
        {
            Id = Guid.NewGuid(),
            CourseId = courseId,
            TeacherId = teacherId,
            Name = name,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(string name, Guid teacherId)
    {
        Name = name;
        TeacherId = teacherId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangeTeacher(Guid teacherId)
    {
        TeacherId = teacherId;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool HasMember(Guid enrollmentId) => _members.Any(m => m.EnrollmentId == enrollmentId);

    public StudyGroupMember AddMember(Guid enrollmentId)
    {
        var member = StudyGroupMember.Create(Id, enrollmentId);
        _members.Add(member);

        return member;
    }

    public StudyGroupMember? RemoveMember(Guid enrollmentId)
    {
        var member = _members.FirstOrDefault(m => m.EnrollmentId == enrollmentId);
        if (member is not null)
            _members.Remove(member);

        return member;
    }
}
