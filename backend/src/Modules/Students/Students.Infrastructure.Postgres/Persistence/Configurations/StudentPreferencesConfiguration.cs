using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Students.Domain.Entities;

namespace Students.Infrastructure.Postgres.Persistence.Configurations;

public class StudentPreferencesConfiguration : IEntityTypeConfiguration<StudentPreferences>
{
    public void Configure(EntityTypeBuilder<StudentPreferences> builder)
    {
        builder.ToTable("student_preferences");

        builder.HasKey(sp => sp.Id);

        builder.Property(sp => sp.Id)
            .ValueGeneratedNever();

        builder.Property(sp => sp.StudentId)
            .IsRequired();

        builder.Property(sp => sp.LearningGoal)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(sp => sp.Format)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(sp => sp.LessonType)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(sp => sp.Intensity)
            .IsRequired();

        builder.Property(sp => sp.CurrentLevel)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(sp => sp.HadPreviousCourses)
            .IsRequired();

        builder.Property(sp => sp.CreatedAt)
            .IsRequired();
    }
}
