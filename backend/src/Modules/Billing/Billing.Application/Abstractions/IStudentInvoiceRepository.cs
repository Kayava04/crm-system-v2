using Billing.Domain.Entities;
using Billing.Domain.Enums;
using Shared.Kernel.Abstractions;

namespace Billing.Application.Abstractions;

public interface IStudentInvoiceRepository : IRepository<StudentInvoice>
{
    Task<bool> ExistsByEnrollmentAndPeriodAsync(
        Guid enrollmentId,
        string period,
        CancellationToken ct = default
    );

    Task<decimal> SumPaidAsync(DateTime from, DateTime to, CancellationToken ct = default);

    Task<decimal> SumByStatusAsync(InvoiceStatus status, CancellationToken ct = default);

    Task<IReadOnlyList<StudentInvoice>> GetPendingDueBeforeAsync(
        DateOnly date,
        CancellationToken ct = default
    );

    Task<(IReadOnlyList<StudentInvoice> Invoices, int TotalCount)> GetAllAsync(
        Guid? studentId,
        Guid? enrollmentId,
        string? period,
        InvoiceStatus? status,
        int page,
        int pageSize,
        CancellationToken ct = default
    );
}
