using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Postgres.Persistence.Configurations;

internal sealed class UserPhotoConfiguration : IEntityTypeConfiguration<UserPhoto>
{
    public void Configure(EntityTypeBuilder<UserPhoto> builder)
    {
        builder.ToTable("user_photos");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .ValueGeneratedNever();

        builder.Property(p => p.UserId)
            .IsRequired();

        // one photo per account
        builder.HasIndex(p => p.UserId)
            .IsUnique();

        builder.Property(p => p.StorageKey)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(p => p.OriginalFileName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(p => p.ContentType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.SizeBytes)
            .IsRequired();

        builder.Property(p => p.UploadedAt)
            .IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
