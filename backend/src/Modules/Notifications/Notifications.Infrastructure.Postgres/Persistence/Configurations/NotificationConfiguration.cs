using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notifications.Domain.Entities;

namespace Notifications.Infrastructure.Postgres.Persistence.Configurations;

internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Id)
            .ValueGeneratedNever();

        builder.Property(n => n.RecipientUserId)
            .IsRequired();

        builder.Property(n => n.Type)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(n => n.Subject)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(n => n.Body)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(n => n.Action)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(n => n.CreatedAt)
            .IsRequired();

        builder.HasIndex(n => new { n.RecipientUserId, n.ReadAt });

        builder.Ignore(n => n.IsRead);
    }
}
