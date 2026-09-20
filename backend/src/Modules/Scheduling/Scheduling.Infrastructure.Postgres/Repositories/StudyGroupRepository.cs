using Microsoft.EntityFrameworkCore;
using Scheduling.Application.Abstractions;
using Scheduling.Domain.Entities;
using Scheduling.Infrastructure.Postgres.Persistence;

namespace Scheduling.Infrastructure.Postgres.Repositories;

internal sealed class StudyGroupRepository(SchedulingDbContext context) : IStudyGroupRepository
{
    public async Task<IReadOnlyList<StudyGroup>> GetAllAsync(CancellationToken ct = default) =>
        await context.StudyGroups
            .AsNoTracking()
            .Include(g => g.Members)
            .OrderBy(g => g.Name)
            .ToListAsync(ct);

    public async Task<StudyGroup?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await context.StudyGroups
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == id, ct);

    public async Task AddAsync(StudyGroup entity, CancellationToken ct = default) =>
        await context.StudyGroups.AddAsync(entity, ct);

    public Task UpdateAsync(StudyGroup entity, CancellationToken ct = default)
    {
        context.StudyGroups.Update(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(StudyGroup entity, CancellationToken ct = default)
    {
        context.StudyGroups.Remove(entity);
        return Task.CompletedTask;
    }

    public async Task<(IReadOnlyList<StudyGroup> Groups, int TotalCount)> GetAllAsync(
        Guid? courseId,
        Guid? teacherId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = context.StudyGroups
            .AsNoTracking()
            .Include(g => g.Members)
            .AsQueryable();

        if (courseId.HasValue)
            query = query.Where(g => g.CourseId == courseId.Value);

        if (teacherId.HasValue)
            query = query.Where(g => g.TeacherId == teacherId.Value);

        var totalCount = await query.CountAsync(ct);

        var groups = await query
            .OrderBy(g => g.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (groups, totalCount);
    }

    public async Task<IReadOnlyList<StudyGroup>> GetByTeacherAsync(Guid teacherId, CancellationToken ct = default) =>
        await context.StudyGroups
            .Where(g => g.TeacherId == teacherId)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<StudyGroup>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken ct = default) =>
        await context.StudyGroups
            .AsNoTracking()
            .Include(g => g.Members)
            .Where(g => ids.Contains(g.Id))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Guid>> GetGroupIdsByEnrollmentsAsync(
        IReadOnlyCollection<Guid> enrollmentIds,
        CancellationToken ct = default) =>
        await context.StudyGroupMembers
            .AsNoTracking()
            .Where(m => enrollmentIds.Contains(m.EnrollmentId))
            .Select(m => m.GroupId)
            .Distinct()
            .ToListAsync(ct);

    public async Task<bool> IsInGroupOfCourseAsync(
        Guid enrollmentId,
        Guid courseId,
        CancellationToken ct = default) =>
        await context.StudyGroupMembers
            .AsNoTracking()
            .AnyAsync(m => m.EnrollmentId == enrollmentId
                && context.StudyGroups.Any(g => g.Id == m.GroupId && g.CourseId == courseId), ct);

    public async Task AddMemberAsync(StudyGroupMember member, CancellationToken ct = default) =>
        await context.StudyGroupMembers.AddAsync(member, ct);

    public Task RemoveMemberAsync(StudyGroupMember member, CancellationToken ct = default)
    {
        context.StudyGroupMembers.Remove(member);
        return Task.CompletedTask;
    }
}
