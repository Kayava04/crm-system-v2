using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Postgres.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.Property(u => u.CreatedAt)
            .IsRequired();

        builder.Property(u => u.FirstName).HasMaxLength(100);
        builder.Property(u => u.LastName).HasMaxLength(100);
        builder.Property(u => u.MiddleName).HasMaxLength(100);
        builder.Property(u => u.City).HasMaxLength(100);
        builder.Property(u => u.Country).HasMaxLength(100);

        builder.Property(u => u.Salary)
            .HasPrecision(18, 2);

        builder.Property(u => u.MustChangePassword)
            .IsRequired();
    }
}
