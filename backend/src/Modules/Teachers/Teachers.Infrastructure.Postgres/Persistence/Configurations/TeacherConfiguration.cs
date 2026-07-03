using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Teachers.Domain.Entities;

namespace Teachers.Infrastructure.Postgres.Persistence.Configurations;

internal sealed class TeacherConfiguration : IEntityTypeConfiguration<Teacher>
{
    public void Configure(EntityTypeBuilder<Teacher> builder)
    {
        builder.ToTable("teachers");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .ValueGeneratedNever();

        builder.Property(t => t.UserId);

        builder.Property(t => t.FirstName)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(t => t.LastName)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(t => t.MiddleName)
            .HasMaxLength(50);

        builder.Property(t => t.DateOfBirth)
            .IsRequired();

        builder.Property(t => t.PhoneNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(t => t.Email)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(t => t.Email)
            .IsUnique();

        builder.Property(t => t.City)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(t => t.Country)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(t => t.Status)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(s => s.Comment)
            .HasMaxLength(500);

        builder.Property(t => t.CreatedAt)
            .IsRequired();

        builder.HasMany(t => t.SalaryRates)
            .WithOne()
            .HasForeignKey(sr => sr.TeacherId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(t => t.SalaryRates)
            .HasField("_salaryRates")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
