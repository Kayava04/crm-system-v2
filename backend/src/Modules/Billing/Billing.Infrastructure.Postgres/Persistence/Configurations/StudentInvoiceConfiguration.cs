using Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Billing.Infrastructure.Postgres.Persistence.Configurations;

internal sealed class StudentInvoiceConfiguration : IEntityTypeConfiguration<StudentInvoice>
{
    public void Configure(EntityTypeBuilder<StudentInvoice> builder)
    {
        builder.ToTable("student_invoices");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id)
            .ValueGeneratedNever();

        builder.Property(i => i.EnrollmentId)
            .IsRequired();

        builder.Property(i => i.StudentId)
            .IsRequired();

        builder.Property(i => i.Period)
            .IsRequired()
            .HasMaxLength(7);

        builder.Property(i => i.Amount)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(i => i.DueDate)
            .IsRequired();

        builder.Property(i => i.Status)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(i => i.Notes)
            .HasMaxLength(500);

        builder.Property(i => i.CreatedAt)
            .IsRequired();

        builder.HasIndex(i => new { i.EnrollmentId, i.Period })
            .IsUnique();

        builder.HasIndex(i => i.StudentId);
    }
}
