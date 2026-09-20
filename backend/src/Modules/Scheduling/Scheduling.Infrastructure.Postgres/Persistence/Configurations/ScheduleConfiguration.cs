using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Scheduling.Domain.Entities;

namespace Scheduling.Infrastructure.Postgres.Persistence.Configurations;

internal sealed class ScheduleConfiguration : IEntityTypeConfiguration<Schedule>
{
    public void Configure(EntityTypeBuilder<Schedule> builder)
    {
        builder.ToTable("schedules");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .ValueGeneratedNever();

        builder.Property(s => s.EnrollmentId)
            .IsRequired();

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

        builder.HasIndex(s => new { s.TeacherId, s.ScheduledDate });

        builder.Ignore(s => s.EndDate);
        builder.Ignore(s => s.IsOpen);
    }
}
