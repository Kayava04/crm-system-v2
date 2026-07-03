using Education.Contracts.Enums;
using Microsoft.EntityFrameworkCore;
using Students.Application.Abstractions;
using Students.Domain.Entities;
using Students.Infrastructure.Postgres.Persistence;

namespace Students.Infrastructure.Postgres.Repositories;

internal sealed class StudentRepository(StudentsDbContext context) : IStudentRepository
{
    public async Task<IReadOnlyList<Student>> GetAllAsync(CancellationToken ct = default) =>
        await context.Students
            .AsNoTracking()
            .OrderBy(s => s.LastName)
            .ThenBy(s => s.FirstName)
            .ToListAsync(ct);

    public async Task<Student?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await context.Students
            .Include(s => s.Preferences)
            .Include(s => s.ParentInfo)
            .Include(s => s.Languages)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task AddAsync(Student entity, CancellationToken ct = default) =>
        await context.Students.AddAsync(entity, ct);

    public Task UpdateAsync(Student entity, CancellationToken ct = default)
    {
        context.Students.Update(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Student entity, CancellationToken ct = default)
    {
        context.Students.Remove(entity);
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default) =>
        await context.Students
            .AsNoTracking()
            .AnyAsync(s => s.Email.ToLower() == email.ToLower(), ct);

    public async Task<(IReadOnlyList<Student> Students, int TotalCount)> GetAllAsync(
        string? search,
        string? city,
        bool? isChild,
        Language? language,
        Level? currentLevel,
        Format? format,
        int page,
        int pageSize,
        CancellationToken ct = default
    )
    {
        var query = context.Students
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
            query = query.Where(s =>
                s.FirstName.ToLower().Contains(search.ToLower()) ||
                s.LastName.ToLower().Contains(search.ToLower())
            );

        if (!string.IsNullOrEmpty(city))
            query = query.Where(s => s.City.ToLower() == city.ToLower());

        if (isChild.HasValue)
            query = query.Where(s => s.IsChild == isChild.Value);

        if (currentLevel.HasValue)
            query = query.Include(s => s.Preferences)
                         .Where(s => s.Preferences!.CurrentLevel == currentLevel.Value);

        if (format.HasValue)
            query = query.Include(s => s.Preferences)
                         .Where(s => s.Preferences!.Format == format.Value);

        if (language.HasValue)
            query = query.Include(s => s.Languages)
                         .Where(s => s.Languages.Any(l => l.Language == language.Value));

        var totalCount = await query.CountAsync(ct);

        var students = await query
            .OrderBy(s => s.LastName)
            .ThenBy(s => s.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (students, totalCount);
    }
}
