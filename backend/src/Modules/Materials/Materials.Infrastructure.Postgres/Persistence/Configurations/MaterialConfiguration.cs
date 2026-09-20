using Materials.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Materials.Infrastructure.Postgres.Persistence.Configurations;

internal sealed class MaterialConfiguration : IEntityTypeConfiguration<Material>
{
    public void Configure(EntityTypeBuilder<Material> builder)
    {
        builder.ToTable("materials");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .ValueGeneratedNever();

        builder.Property(m => m.CourseId)
            .IsRequired();

        builder.Property(m => m.AuthorUserId)
            .IsRequired();

        builder.Property(m => m.Type)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(m => m.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(m => m.Description)
            .HasMaxLength(1000);

        builder.Property(m => m.Body)
            .HasMaxLength(20000);

        builder.Property(m => m.Url)
            .HasMaxLength(2000);

        builder.Property(m => m.YouTubeVideoId)
            .HasMaxLength(11);

        builder.Property(m => m.CreatedAt)
            .IsRequired();

        builder.HasIndex(m => m.CourseId);
    }
}
