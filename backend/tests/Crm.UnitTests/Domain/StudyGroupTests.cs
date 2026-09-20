using Scheduling.Domain.Entities;

namespace Crm.UnitTests.Domain;

public class StudyGroupTests
{
    private static StudyGroup NewGroup() => StudyGroup.Create(Guid.NewGuid(), Guid.NewGuid(), "Group A");

    [Fact]
    public void New_group_has_no_members()
    {
        Assert.Empty(NewGroup().Members);
    }

    [Fact]
    public void AddMember_adds_the_enrollment_to_the_group()
    {
        var group = NewGroup();
        var enrollmentId = Guid.NewGuid();

        var member = group.AddMember(enrollmentId);

        Assert.Equal(group.Id, member.GroupId);
        Assert.Equal(enrollmentId, member.EnrollmentId);
        Assert.True(group.HasMember(enrollmentId));
        Assert.Single(group.Members);
    }

    [Fact]
    public void RemoveMember_returns_the_removed_member()
    {
        var group = NewGroup();
        var enrollmentId = Guid.NewGuid();
        group.AddMember(enrollmentId);

        var removed = group.RemoveMember(enrollmentId);

        Assert.NotNull(removed);
        Assert.False(group.HasMember(enrollmentId));
    }

    [Fact]
    public void RemoveMember_of_a_stranger_returns_null()
    {
        Assert.Null(NewGroup().RemoveMember(Guid.NewGuid()));
    }

    [Fact]
    public void Update_and_ChangeTeacher_change_the_group()
    {
        var group = NewGroup();
        var teacher = Guid.NewGuid();

        group.Update("Renamed", teacher);
        Assert.Equal("Renamed", group.Name);
        Assert.Equal(teacher, group.TeacherId);

        var other = Guid.NewGuid();
        group.ChangeTeacher(other);
        Assert.Equal(other, group.TeacherId);
        Assert.NotNull(group.UpdatedAt);
    }
}
