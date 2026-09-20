using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Students.Domain.Entities;

namespace Students.Infrastructure.Postgres.Persistence.Configurations;

internal sealed class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.ToTable("students");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .ValueGeneratedNever();

        builder.Property(s => s.UserId);

        builder.Property(s => s.FirstName)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(s => s.LastName)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(s => s.MiddleName)
            .HasMaxLength(50);

        builder.Property(s => s.DateOfBirth)
            .IsRequired();

        builder.Property(s => s.PhoneNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(s => s.Email)
            .IsRequired()
            .HasMaxLength(100);

        // "which student is this account?" is asked on every calendar request
        builder.HasIndex(s => s.UserId);

        builder.HasIndex(s => s.Email)
            .IsUnique();

        builder.Property(s => s.City)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(s => s.Country)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(s => s.IsChild)
            .IsRequired();

        builder.Property(s => s.Status)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(s => s.Comment)
            .HasMaxLength(500);

        builder.Property(s => s.CreatedAt)
            .IsRequired();

        builder.HasOne(s => s.Preferences)
            .WithOne()
            .HasForeignKey<StudentPreferences>(sp => sp.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.ParentInfo)
            .WithOne()
            .HasForeignKey<ParentInfo>(p => p.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.Languages)
            .WithOne()
            .HasForeignKey(sl => sl.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(s => s.Languages)
            .HasField("_languages")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
