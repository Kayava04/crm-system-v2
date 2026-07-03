using Courses.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Courses.Infrastructure.Postgres.Persistence.Configurations;

internal sealed class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.ToTable("courses");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .ValueGeneratedNever();

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(c => c.Name)
            .IsUnique();

        builder.Property(c => c.Language)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(c => c.Level)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(c => c.Format)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(c => c.LessonType)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(c => c.DurationMonths)
            .IsRequired();

        builder.Property(c => c.LessonsCount)
            .IsRequired();

        builder.Property(c => c.LessonsPerWeek)
            .IsRequired();

        builder.Property(c => c.Price)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(c => c.Description)
            .HasMaxLength(1000);

        builder.Property(c => c.Status)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(c => c.CreatedAt)
            .IsRequired();
    }
}
