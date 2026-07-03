using Microsoft.EntityFrameworkCore;
using Teachers.Application.Abstractions;
using Teachers.Domain.Entities;

namespace Teachers.Infrastructure.Postgres.Persistence;

internal sealed class TeachersDbContext(DbContextOptions<TeachersDbContext> options)
    : DbContext(options), ITeacherUnitOfWork
{
    public DbSet<Teacher> Teachers => Set<Teacher>();
    public DbSet<TeacherSalaryRate> TeacherSalaryRates => Set<TeacherSalaryRate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("teachers");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TeachersDbContext).Assembly);
    }
}
