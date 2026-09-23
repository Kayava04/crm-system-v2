using Billing.Domain.Enums;
using Shared.Kernel.Primitives;

namespace Billing.Domain.Entities;

// A payroll entry belongs either to a teacher (BaseSalary/LessonsRate computed from lessons taught
// that period) or to any other staff account (an administrator or manager, whose pay is simply their
// User.Salary for the period - no lessons involved, so LessonsRate and CompletedLessonsCount stay 0).
// Kept as one entity/table so staff gets the exact same accrual-with-history mechanism teachers
// already had, rather than a parallel feature.
public sealed class TeacherPayroll : AuditableEntity
{
    public Guid? TeacherId { get; private set; }
    public Guid? UserId { get; private set; }
    public string Period { get; private set; } = string.Empty;
    public decimal BaseSalary { get; private set; }
    public decimal LessonsRate { get; private set; }
    public int CompletedLessonsCount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public PayrollStatus Status { get; private set; }
    public DateTime? PaidAt { get; private set; }

    private TeacherPayroll() { }

    public static TeacherPayroll CreateForTeacher(
        Guid teacherId,
        string period,
        decimal baseSalary,
        decimal lessonsRate,
        int completedLessonsCount
    ) => Create(teacherId, null, period, baseSalary, lessonsRate, completedLessonsCount);

    public static TeacherPayroll CreateForStaff(Guid userId, string period, decimal salary) =>
        Create(null, userId, period, salary, lessonsRate: 0, completedLessonsCount: 0);

    private static TeacherPayroll Create(
        Guid? teacherId,
        Guid? userId,
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
            UserId = userId,
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
