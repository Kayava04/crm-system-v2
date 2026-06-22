using Microsoft.EntityFrameworkCore;
using Shared.Kernel.Abstractions;
using Students.Domain.Entities;

namespace Students.Infrastructure.Postgres.Persistence;

internal sealed class StudentsDbContext(DbContextOptions<StudentsDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<Student> Students => Set<Student>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("students");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(StudentsDbContext).Assembly);
    }
}
