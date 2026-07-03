using Courses.Application.Abstractions;
using Courses.Domain.Entities;
using Courses.Domain.Enums;
using Education.Contracts.Enums;
using Courses.Infrastructure.Postgres.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Courses.Infrastructure.Postgres.Repositories;

internal sealed class CourseRepository(CoursesDbContext context) : ICourseRepository
{
    public async Task<IReadOnlyList<Course>> GetAllAsync(CancellationToken ct = default) =>
        await context.Courses
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync(ct);

    public async Task<Course?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await context.Courses
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task AddAsync(Course entity, CancellationToken ct = default) =>
        await context.Courses.AddAsync(entity, ct);

    public Task UpdateAsync(Course entity, CancellationToken ct = default)
    {
        context.Courses.Update(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Course entity, CancellationToken ct = default)
    {
        context.Courses.Remove(entity);
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default) =>
        await context.Courses
            .AsNoTracking()
            .AnyAsync(c => c.Name.ToLower() == name.ToLower(), ct);

    public async Task<(IReadOnlyList<Course> Courses, int TotalCount)> GetAllAsync(
        string? search,
        Language? language,
        Level? level,
        Format? format,
        LessonType? lessonType,
        CourseStatus? status,
        int page,
        int pageSize,
        CancellationToken ct = default
    )
    {
        var query = context.Courses
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
            query = query.Where(c => c.Name.ToLower().Contains(search.ToLower()));

        if (language.HasValue)
            query = query.Where(c => c.Language == language.Value);

        if (level.HasValue)
            query = query.Where(c => c.Level == level.Value);

        if (format.HasValue)
            query = query.Where(c => c.Format == format.Value);

        if (lessonType.HasValue)
            query = query.Where(c => c.LessonType == lessonType.Value);

        if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);

        var totalCount = await query.CountAsync(ct);

        var courses = await query
            .OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (courses, totalCount);
    }
}
