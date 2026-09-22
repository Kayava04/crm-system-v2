using Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Billing.Infrastructure.Postgres.Persistence.Configurations;

internal sealed class TeacherPayrollConfiguration : IEntityTypeConfiguration<TeacherPayroll>
{
    public void Configure(EntityTypeBuilder<TeacherPayroll> builder)
    {
        builder.ToTable("teacher_payrolls", t => t.HasCheckConstraint(
            "ck_teacher_payrolls_target",
            "(\"TeacherId\" IS NOT NULL) <> (\"UserId\" IS NOT NULL)"));

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .ValueGeneratedNever();

        builder.Property(p => p.TeacherId);

        builder.Property(p => p.UserId);

        builder.Property(p => p.Period)
            .IsRequired()
            .HasMaxLength(7);

        builder.Property(p => p.BaseSalary)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(p => p.LessonsRate)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(p => p.CompletedLessonsCount)
            .IsRequired();

        builder.Property(p => p.TotalAmount)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(p => p.Status)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        builder.HasIndex(p => new { p.TeacherId, p.Period })
            .IsUnique();

        builder.HasIndex(p => new { p.UserId, p.Period })
            .IsUnique();
    }
}
