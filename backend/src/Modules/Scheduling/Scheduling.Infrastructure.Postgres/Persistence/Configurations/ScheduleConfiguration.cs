using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Scheduling.Domain.Entities;

namespace Scheduling.Infrastructure.Postgres.Persistence.Configurations;

internal sealed class ScheduleConfiguration : IEntityTypeConfiguration<Schedule>
{
    public void Configure(EntityTypeBuilder<Schedule> builder)
    {
        builder.ToTable("schedules", t => t.HasCheckConstraint(
            "ck_schedules_target",
            "(\"EnrollmentId\" IS NOT NULL) <> (\"GroupId\" IS NOT NULL)"));

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .ValueGeneratedNever();

        builder.Property(s => s.EnrollmentId);

        builder.Property(s => s.GroupId);

        builder.Property(s => s.TeacherId)
            .IsRequired();

        builder.Property(s => s.ScheduledDate)
            .IsRequired();

        builder.Property(s => s.DurationMinutes)
            .IsRequired();

        builder.Property(s => s.Status)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(s => s.Notes)
            .HasMaxLength(500);

        builder.Property(s => s.CreatedAt)
            .IsRequired();

        builder.HasIndex(s => s.EnrollmentId);

        builder.HasIndex(s => s.GroupId);

        builder.HasIndex(s => new { s.TeacherId, s.ScheduledDate });

        builder.Ignore(s => s.EndDate);
        builder.Ignore(s => s.IsOpen);
    }
}
