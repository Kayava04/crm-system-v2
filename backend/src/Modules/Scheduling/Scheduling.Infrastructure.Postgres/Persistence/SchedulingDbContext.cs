using Microsoft.EntityFrameworkCore;
using Scheduling.Application.Abstractions;
using Scheduling.Domain.Entities;

namespace Scheduling.Infrastructure.Postgres.Persistence;

internal sealed class SchedulingDbContext(DbContextOptions<SchedulingDbContext> options)
    : DbContext(options), ISchedulingUnitOfWork
{
    public DbSet<Schedule> Schedules => Set<Schedule>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("scheduling");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SchedulingDbContext).Assembly);
    }
}
