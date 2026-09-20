using Microsoft.EntityFrameworkCore;
using Teachers.Application.Abstractions;
using Teachers.Domain.Entities;
using Teachers.Domain.Enums;
using Teachers.Infrastructure.Postgres.Persistence;

namespace Teachers.Infrastructure.Postgres.Repositories;

internal sealed class TeacherRepository(TeachersDbContext context) : ITeacherRepository
{
    public async Task<IReadOnlyList<Teacher>> GetAllAsync(CancellationToken ct = default) =>
        await context.Teachers
            .AsNoTracking()
            .OrderBy(s => s.LastName)
            .ThenBy(s => s.FirstName)
            .ToListAsync(ct);

    public async Task<Teacher?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await context.Teachers
            .Include(t => t.SalaryRates)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task AddAsync(Teacher entity, CancellationToken ct = default) =>
        await context.Teachers.AddAsync(entity, ct);

    public Task UpdateAsync(Teacher entity, CancellationToken ct = default)
    {
        context.Teachers.Update(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Teacher entity, CancellationToken ct = default)
    {
        context.Teachers.Remove(entity);
        return Task.CompletedTask;
    }

    public async Task<Teacher?> GetByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        await context.Teachers
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.UserId == userId, ct);

    public async Task<bool> ExistsByIdAsync(Guid id, CancellationToken ct = default) =>
        await context.Teachers
            .AsNoTracking()
            .AnyAsync(t => t.Id == id, ct);

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default) =>
        await context.Teachers
            .AsNoTracking()
            .AnyAsync(t => t.Email.ToLower() == email.ToLower(), ct);

    public async Task<(IReadOnlyList<Teacher> Teachers, int TotalCount)> GetAllAsync(
        string? search,
        string? city,
        TeacherStatus? status,
        int page,
        int pageSize,
        CancellationToken ct = default
    )
    {
        var query = context.Teachers
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
            query = query.Where(t =>
                t.FirstName.ToLower().Contains(search.ToLower()) ||
                t.LastName.ToLower().Contains(search.ToLower())
            );

        if (!string.IsNullOrEmpty(city))
            query = query.Where(t => t.City.ToLower() == city.ToLower());

        if (status.HasValue)
            query = query.Where(t => t.Status == status.Value);

        var totalCount = await query.CountAsync(ct);

        var teachers = await query
            .OrderBy(t => t.LastName)
            .ThenBy(t => t.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (teachers, totalCount);
    }

    public async Task<IReadOnlyList<Teacher>> GetAllWithSalaryRatesAsync(CancellationToken ct = default) =>
        await context.Teachers
            .AsNoTracking()
            .Include(t => t.SalaryRates)
            .ToListAsync(ct);
}
