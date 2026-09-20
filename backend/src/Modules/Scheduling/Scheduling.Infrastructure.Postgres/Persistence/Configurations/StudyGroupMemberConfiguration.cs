using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Scheduling.Domain.Entities;

namespace Scheduling.Infrastructure.Postgres.Persistence.Configurations;

internal sealed class StudyGroupMemberConfiguration : IEntityTypeConfiguration<StudyGroupMember>
{
    public void Configure(EntityTypeBuilder<StudyGroupMember> builder)
    {
        builder.ToTable("study_group_members");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .ValueGeneratedNever();

        builder.Property(m => m.GroupId)
            .IsRequired();

        builder.Property(m => m.EnrollmentId)
            .IsRequired();

        builder.Property(m => m.JoinedAt)
            .IsRequired();

        builder.HasIndex(m => new { m.GroupId, m.EnrollmentId })
            .IsUnique();

        builder.HasIndex(m => m.EnrollmentId);
    }
}
