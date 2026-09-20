using Billing.Application.Abstractions;
using Billing.Domain.Entities;
using Billing.Domain.Enums;
using Billing.Infrastructure.Postgres.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Billing.Infrastructure.Postgres.Repositories;

internal sealed class StudentInvoiceRepository(BillingDbContext context) : IStudentInvoiceRepository
{
    public async Task<IReadOnlyList<StudentInvoice>> GetAllAsync(CancellationToken ct = default) =>
        await context.StudentInvoices
            .AsNoTracking()
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(ct);

    public async Task<StudentInvoice?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await context.StudentInvoices
            .FirstOrDefaultAsync(i => i.Id == id, ct);

    public async Task AddAsync(StudentInvoice entity, CancellationToken ct = default) =>
        await context.StudentInvoices.AddAsync(entity, ct);

    public Task UpdateAsync(StudentInvoice entity, CancellationToken ct = default)
    {
        context.StudentInvoices.Update(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(StudentInvoice entity, CancellationToken ct = default)
    {
        context.StudentInvoices.Remove(entity);
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsByEnrollmentAndPeriodAsync(
        Guid enrollmentId,
        string period,
        CancellationToken ct = default) =>
        await context.StudentInvoices
            .AsNoTracking()
            .AnyAsync(i => i.EnrollmentId == enrollmentId && i.Period == period, ct);

    public async Task<IReadOnlyList<StudentInvoice>> GetPendingDueBeforeAsync(
        DateOnly date,
        CancellationToken ct = default) =>
        await context.StudentInvoices
            .Where(i => i.Status == InvoiceStatus.Pending && i.DueDate < date)
            .ToListAsync(ct);

    public async Task<(IReadOnlyList<StudentInvoice> Invoices, int TotalCount)> GetAllAsync(
        Guid? studentId,
        Guid? enrollmentId,
        string? period,
        InvoiceStatus? status,
        int page,
        int pageSize,
        CancellationToken ct = default
    )
    {
        var query = context.StudentInvoices
            .AsNoTracking()
            .AsQueryable();

        if (studentId.HasValue)
            query = query.Where(i => i.StudentId == studentId.Value);

        if (enrollmentId.HasValue)
            query = query.Where(i => i.EnrollmentId == enrollmentId.Value);

        if (!string.IsNullOrWhiteSpace(period))
            query = query.Where(i => i.Period == period);

        if (status.HasValue)
            query = query.Where(i => i.Status == status.Value);

        var totalCount = await query.CountAsync(ct);

        var invoices = await query
            .OrderByDescending(i => i.DueDate)
            .ThenByDescending(i => i.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (invoices, totalCount);
    }

    public async Task<decimal> SumPaidAsync(DateTime from, DateTime to, CancellationToken ct = default) =>
        await context.StudentInvoices
            .AsNoTracking()
            .Where(i => i.Status == InvoiceStatus.Paid && i.PaidAt >= from && i.PaidAt < to)
            .SumAsync(i => (decimal?)i.Amount, ct) ?? 0m;

    public async Task<decimal> SumByStatusAsync(InvoiceStatus status, CancellationToken ct = default) =>
        await context.StudentInvoices
            .AsNoTracking()
            .Where(i => i.Status == status)
            .SumAsync(i => (decimal?)i.Amount, ct) ?? 0m;
}
