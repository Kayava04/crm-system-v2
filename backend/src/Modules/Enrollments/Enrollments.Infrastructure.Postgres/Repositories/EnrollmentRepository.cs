using Enrollments.Application.Abstractions;
using Enrollments.Domain.Entities;
using Enrollments.Domain.Enums;
using Enrollments.Infrastructure.Postgres.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Enrollments.Infrastructure.Postgres.Repositories;

internal sealed class EnrollmentRepository(EnrollmentsDbContext context) : IEnrollmentRepository
{
    public async Task<IReadOnlyList<Enrollment>> GetAllAsync(CancellationToken ct = default) =>
        await context.Enrollments
            .AsNoTracking()
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(ct);

    public async Task<Enrollment?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await context.Enrollments
            .FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task AddAsync(Enrollment entity, CancellationToken ct = default) =>
        await context.Enrollments.AddAsync(entity, ct);

    public Task UpdateAsync(Enrollment entity, CancellationToken ct = default)
    {
        context.Enrollments.Update(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Enrollment entity, CancellationToken ct = default)
    {
        context.Enrollments.Remove(entity);
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsByStudentAndCourseAsync(
        Guid studentId,
        Guid courseId,
        CancellationToken ct = default) =>
        await context.Enrollments
            .AsNoTracking()
            .AnyAsync(e => e.StudentId == studentId
                && e.CourseId == courseId
                && e.Status != EnrollmentStatus.Terminated
                && e.Status != EnrollmentStatus.Completed, ct
            );

    public async Task<IReadOnlyList<Guid>> GetStudentIdsByEnrollmentIdsAsync(
        IReadOnlyCollection<Guid> enrollmentIds,
        CancellationToken ct = default) =>
        await context.Enrollments
            .AsNoTracking()
            .Where(e => enrollmentIds.Contains(e.Id))
            .Select(e => e.StudentId)
            .Distinct()
            .ToListAsync(ct);

    public async Task<(IReadOnlyList<Enrollment> Enrollments, int TotalCount)> GetAllAsync(
        Guid? studentId,
        Guid? courseId,
        EnrollmentStatus? status,
        int page,
        int pageSize,
        CancellationToken ct = default
    )
    {
        var query = context.Enrollments
            .AsNoTracking()
            .AsQueryable();

        if (studentId.HasValue)
            query = query.Where(e => e.StudentId == studentId.Value);

        if (courseId.HasValue)
            query = query.Where(e => e.CourseId == courseId.Value);

        if (status.HasValue)
            query = query.Where(e => e.Status == status.Value);

        var totalCount = await query.CountAsync(ct);

        var enrollments = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (enrollments, totalCount);
    }

    public async Task<IReadOnlyDictionary<EnrollmentStatus, int>> GetCountsByStatusAsync(CancellationToken ct = default) =>
        await context.Enrollments
            .AsNoTracking()
            .GroupBy(e => e.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count, ct);

    public async Task<IReadOnlyDictionary<Guid, int>> GetCountsByCourseAsync(CancellationToken ct = default) =>
        await context.Enrollments
            .AsNoTracking()
            .GroupBy(e => e.CourseId)
            .Select(g => new { CourseId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CourseId, x => x.Count, ct);

    public async Task<IReadOnlyList<Enrollment>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken ct = default) =>
        await context.Enrollments
            .AsNoTracking()
            .Where(e => ids.Contains(e.Id))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Enrollment>> GetActiveByStudentAsync(Guid studentId, CancellationToken ct = default) =>
        await context.Enrollments
            .Where(e => e.StudentId == studentId && e.Status == EnrollmentStatus.Active)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Enrollment>> GetAutoSuspendedByStudentAsync(Guid studentId, CancellationToken ct = default) =>
        await context.Enrollments
            .Where(e => e.StudentId == studentId && e.Status == EnrollmentStatus.Suspended && e.AutoSuspended)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Enrollment>> GetByStudentAsync(Guid studentId, CancellationToken ct = default) =>
        await context.Enrollments
            .AsNoTracking()
            .Where(e => e.StudentId == studentId)
            .ToListAsync(ct);

    public async Task<IReadOnlySet<Guid>> GetStudentIdsWithEnrollmentsAsync(
        IReadOnlyCollection<Guid> studentIds,
        CancellationToken ct = default) =>
        (await context.Enrollments
            .AsNoTracking()
            .Where(e => studentIds.Contains(e.StudentId))
            .Select(e => e.StudentId)
            .Distinct()
            .ToListAsync(ct)).ToHashSet();

    public async Task<IReadOnlyList<Guid>> GetIdsByStudentAsync(Guid studentId, CancellationToken ct = default) =>
        await context.Enrollments
            .AsNoTracking()
            .Where(e => e.StudentId == studentId)
            .Select(e => e.Id)
            .ToListAsync(ct);
}
