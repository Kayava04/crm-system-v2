using Enrollments.Application.Abstractions;
using Enrollments.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Enrollments.Infrastructure.Postgres.Persistence;

internal sealed class EnrollmentsDbContext(DbContextOptions<EnrollmentsDbContext> options)
    : DbContext(options), IEnrollmentUnitOfWork
{
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("enrollments");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EnrollmentsDbContext).Assembly);
    }
}
