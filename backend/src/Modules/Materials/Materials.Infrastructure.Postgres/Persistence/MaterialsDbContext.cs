using Materials.Application.Abstractions;
using Materials.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Materials.Infrastructure.Postgres.Persistence;

internal sealed class MaterialsDbContext(DbContextOptions<MaterialsDbContext> options)
    : DbContext(options), IMaterialsUnitOfWork
{
    public DbSet<Material> Materials => Set<Material>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("materials");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MaterialsDbContext).Assembly);
    }
}
