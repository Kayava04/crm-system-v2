using Billing.Domain.Entities;
using Billing.Domain.Enums;
using Shared.Kernel.Abstractions;

namespace Billing.Application.Abstractions;

public interface ITeacherPayrollRepository : IRepository<TeacherPayroll>
{
    Task<bool> ExistsByTeacherAndPeriodAsync(
        Guid teacherId,
        string period,
        CancellationToken ct = default
    );

    Task<bool> ExistsByUserAndPeriodAsync(
        Guid userId,
        string period,
        CancellationToken ct = default
    );

    Task<decimal> SumPaidAsync(DateTime from, DateTime to, CancellationToken ct = default);

    Task<decimal> SumByStatusAsync(PayrollStatus status, CancellationToken ct = default);

    Task<(IReadOnlyList<TeacherPayroll> Payrolls, int TotalCount)> GetAllAsync(
        Guid? teacherId,
        Guid? userId,
        string? period,
        PayrollStatus? status,
        int page,
        int pageSize,
        CancellationToken ct = default
    );
}
