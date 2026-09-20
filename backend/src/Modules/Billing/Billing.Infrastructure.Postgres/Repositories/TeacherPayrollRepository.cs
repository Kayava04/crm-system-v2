using Billing.Application.Abstractions;
using Billing.Domain.Entities;
using Billing.Domain.Enums;
using Billing.Infrastructure.Postgres.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Billing.Infrastructure.Postgres.Repositories;

internal sealed class TeacherPayrollRepository(BillingDbContext context) : ITeacherPayrollRepository
{
    public async Task<IReadOnlyList<TeacherPayroll>> GetAllAsync(CancellationToken ct = default) =>
        await context.TeacherPayrolls
            .AsNoTracking()
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);

    public async Task<TeacherPayroll?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await context.TeacherPayrolls
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task AddAsync(TeacherPayroll entity, CancellationToken ct = default) =>
        await context.TeacherPayrolls.AddAsync(entity, ct);

    public Task UpdateAsync(TeacherPayroll entity, CancellationToken ct = default)
    {
        context.TeacherPayrolls.Update(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(TeacherPayroll entity, CancellationToken ct = default)
    {
        context.TeacherPayrolls.Remove(entity);
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsByTeacherAndPeriodAsync(
        Guid teacherId,
        string period,
        CancellationToken ct = default) =>
        await context.TeacherPayrolls
            .AsNoTracking()
            .AnyAsync(p => p.TeacherId == teacherId && p.Period == period, ct);

    public async Task<(IReadOnlyList<TeacherPayroll> Payrolls, int TotalCount)> GetAllAsync(
        Guid? teacherId,
        string? period,
        PayrollStatus? status,
        int page,
        int pageSize,
        CancellationToken ct = default
    )
    {
        var query = context.TeacherPayrolls
            .AsNoTracking()
            .AsQueryable();

        if (teacherId.HasValue)
            query = query.Where(p => p.TeacherId == teacherId.Value);

        if (!string.IsNullOrWhiteSpace(period))
            query = query.Where(p => p.Period == period);

        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);

        var totalCount = await query.CountAsync(ct);

        var payrolls = await query
            .OrderByDescending(p => p.Period)
            .ThenByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (payrolls, totalCount);
    }

    public async Task<decimal> SumPaidAsync(DateTime from, DateTime to, CancellationToken ct = default) =>
        await context.TeacherPayrolls
            .AsNoTracking()
            .Where(p => p.Status == PayrollStatus.Paid && p.PaidAt >= from && p.PaidAt < to)
            .SumAsync(p => (decimal?)p.TotalAmount, ct) ?? 0m;

    public async Task<decimal> SumByStatusAsync(PayrollStatus status, CancellationToken ct = default) =>
        await context.TeacherPayrolls
            .AsNoTracking()
            .Where(p => p.Status == status)
            .SumAsync(p => (decimal?)p.TotalAmount, ct) ?? 0m;
}
