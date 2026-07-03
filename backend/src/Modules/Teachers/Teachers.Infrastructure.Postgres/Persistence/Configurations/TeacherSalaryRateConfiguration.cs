using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Teachers.Domain.Entities;

namespace Teachers.Infrastructure.Postgres.Persistence.Configurations;

internal sealed class TeacherSalaryRateConfiguration : IEntityTypeConfiguration<TeacherSalaryRate>
{
    public void Configure(EntityTypeBuilder<TeacherSalaryRate> builder)
    {
        builder.ToTable("teacher_salary_rates");

        builder.HasKey(sr => sr.Id);

        builder.Property(sr => sr.Id)
            .ValueGeneratedNever();

        builder.Property(sr => sr.TeacherId)
            .IsRequired();

        builder.Property(sr => sr.BaseSalary)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(sr => sr.LessonsRate)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(sr => sr.EffectiveFrom)
            .IsRequired();

        builder.Property(sr => sr.CreatedAt)
            .IsRequired();
    }
}
