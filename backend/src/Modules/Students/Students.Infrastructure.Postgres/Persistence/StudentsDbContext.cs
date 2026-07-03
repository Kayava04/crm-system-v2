using Microsoft.EntityFrameworkCore;
using Students.Application.Abstractions;
using Students.Domain.Entities;

namespace Students.Infrastructure.Postgres.Persistence;

internal sealed class StudentsDbContext(DbContextOptions<StudentsDbContext> options)
    : DbContext(options), IStudentUnitOfWork
{
    public DbSet<Student> Students => Set<Student>();
    public DbSet<StudentPreferences> StudentPreferences => Set<StudentPreferences>();
    public DbSet<ParentInfo> ParentInfo => Set<ParentInfo>();
    public DbSet<StudentLanguage> StudentLanguages => Set<StudentLanguage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("students");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(StudentsDbContext).Assembly);
    }
}
