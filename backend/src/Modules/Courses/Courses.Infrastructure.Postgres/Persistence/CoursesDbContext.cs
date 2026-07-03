using Courses.Application.Abstractions;
using Courses.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Courses.Infrastructure.Postgres.Persistence;

internal sealed class CoursesDbContext(DbContextOptions<CoursesDbContext> options)
    : DbContext(options), ICoursesUnitOfWork
{
    public DbSet<Course> Courses => Set<Course>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("courses");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CoursesDbContext).Assembly);
    }
}
