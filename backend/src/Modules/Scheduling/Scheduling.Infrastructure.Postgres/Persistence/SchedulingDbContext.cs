using Microsoft.EntityFrameworkCore;
using Scheduling.Application.Abstractions;
using Scheduling.Domain.Entities;

namespace Scheduling.Infrastructure.Postgres.Persistence;

internal sealed class SchedulingDbContext(DbContextOptions<SchedulingDbContext> options)
    : DbContext(options), ISchedulingUnitOfWork
{
    public DbSet<Schedule> Schedules => Set<Schedule>();
    public DbSet<StudyGroup> StudyGroups => Set<StudyGroup>();
    public DbSet<StudyGroupMember> StudyGroupMembers => Set<StudyGroupMember>();
    public DbSet<CalendarEvent> CalendarEvents => Set<CalendarEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("scheduling");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SchedulingDbContext).Assembly);
    }
}
