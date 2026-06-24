using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Students.Domain.Entities;

namespace Students.Infrastructure.Postgres.Persistence.Configurations;

internal sealed class StudentLanguageConfiguration : IEntityTypeConfiguration<StudentLanguage>
{
    public void Configure(EntityTypeBuilder<StudentLanguage> builder)
    {
        builder.ToTable("student_languages");

        builder.HasKey(sl => new { sl.StudentId, sl.Language });

        builder.Property(sl => sl.Language)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(sl => sl.CreatedAt)
            .IsRequired();
    }
}
