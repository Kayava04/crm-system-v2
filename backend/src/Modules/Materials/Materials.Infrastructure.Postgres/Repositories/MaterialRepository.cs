using Materials.Application.Abstractions;
using Materials.Domain.Entities;
using Materials.Domain.Enums;
using Materials.Infrastructure.Postgres.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Materials.Infrastructure.Postgres.Repositories;

internal sealed class MaterialRepository(MaterialsDbContext context) : IMaterialRepository
{
    public async Task<IReadOnlyList<Material>> GetAllAsync(CancellationToken ct = default) =>
        await context.Materials
            .AsNoTracking()
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(ct);

    public async Task<Material?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await context.Materials
            .FirstOrDefaultAsync(m => m.Id == id, ct);

    public async Task AddAsync(Material entity, CancellationToken ct = default) =>
        await context.Materials.AddAsync(entity, ct);

    public Task UpdateAsync(Material entity, CancellationToken ct = default)
    {
        context.Materials.Update(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Material entity, CancellationToken ct = default)
    {
        context.Materials.Remove(entity);
        return Task.CompletedTask;
    }

    public async Task<(IReadOnlyList<Material> Materials, int TotalCount)> GetAllAsync(
        Guid? courseId,
        MaterialType? type,
        string? search,
        int page,
        int pageSize,
        CancellationToken ct = default
    )
    {
        var query = context.Materials
            .AsNoTracking()
            .AsQueryable();

        if (courseId.HasValue)
            query = query.Where(m => m.CourseId == courseId.Value);

        if (type.HasValue)
            query = query.Where(m => m.Type == type.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(m => m.Title.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(ct);

        var materials = await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (materials, totalCount);
    }
}
