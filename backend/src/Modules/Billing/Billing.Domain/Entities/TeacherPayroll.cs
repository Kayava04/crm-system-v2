using Billing.Domain.Enums;
using Shared.Kernel.Primitives;

namespace Billing.Domain.Entities;

public sealed class TeacherPayroll : AuditableEntity
{
    public Guid TeacherId { get; private set; }
    public string Period { get; private set; } = string.Empty;
    public decimal BaseSalary { get; private set; }
    public decimal LessonsRate { get; private set; }
    public int CompletedLessonsCount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public PayrollStatus Status { get; private set; }
    public DateTime? PaidAt { get; private set; }

    private TeacherPayroll() { }

    public static TeacherPayroll Create(
        Guid teacherId,
        string period,
        decimal baseSalary,
        decimal lessonsRate,
        int completedLessonsCount
    )
    {
        return new TeacherPayroll
        {
            Id = Guid.NewGuid(),
            TeacherId = teacherId,
            Period = period,
            BaseSalary = baseSalary,
            LessonsRate = lessonsRate,
            CompletedLessonsCount = completedLessonsCount,
            TotalAmount = baseSalary + lessonsRate * completedLessonsCount,
            Status = PayrollStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkPaid()
    {
        Status = PayrollStatus.Paid;
        PaidAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
