using Microsoft.EntityFrameworkCore;
using Shared.Kernel.Abstractions;
using Students.Domain.Entities;
using Students.Infrastructure.Postgres.Persistence;

namespace Students.Infrastructure.Postgres.Repositories;

internal sealed class StudentRepository(StudentsDbContext context) : IRepository<Student>
{
    public async Task<IReadOnlyList<Student>> GetAllAsync(CancellationToken ct = default) =>
        await context.Students
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task<Student?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await context.Students
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task AddAsync(Student entity, CancellationToken ct = default) =>
        await context.Students.AddAsync(entity, ct);

    public async Task UpdateAsync(Student entity, CancellationToken ct = default)
    {
        context.Students.Update(entity);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(Student entity, CancellationToken ct = default)
    {
        context.Students.Remove(entity);
        await Task.CompletedTask;
    }
}
