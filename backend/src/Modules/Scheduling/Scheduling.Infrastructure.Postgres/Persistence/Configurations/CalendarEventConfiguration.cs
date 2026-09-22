using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Scheduling.Domain.Entities;

namespace Scheduling.Infrastructure.Postgres.Persistence.Configurations;

internal sealed class CalendarEventConfiguration : IEntityTypeConfiguration<CalendarEvent>
{
    public void Configure(EntityTypeBuilder<CalendarEvent> builder)
    {
        builder.ToTable("calendar_events");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedNever();

        builder.Property(e => e.CreatedByUserId)
            .IsRequired();

        builder.Property(e => e.Visibility)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Description)
            .HasMaxLength(2000);

        builder.Property(e => e.StartsAt)
            .IsRequired();

        builder.Property(e => e.EndsAt)
            .IsRequired();

        builder.Property(e => e.IsAllDay)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        // The calendar's own list query always filters by owner-or-everyone and by the time range
        builder.HasIndex(e => new { e.CreatedByUserId, e.StartsAt });

        builder.HasIndex(e => new { e.Visibility, e.StartsAt });
    }
}
