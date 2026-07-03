using Shared.Kernel.Primitives;

namespace Teachers.Domain.Entities;

public sealed class TeacherSalaryRate : Entity
{
    public Guid TeacherId { get; private set; }
    public decimal BaseSalary { get; private set; }
    public decimal LessonsRate { get; private set; }
    public DateTime EffectiveFrom { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private TeacherSalaryRate() { }

    public static TeacherSalaryRate Create(
        Guid teacherId,
        decimal baseSalary,
        decimal lessonsRate,
        DateTime effectiveFrom
    )
    {
        return new TeacherSalaryRate
        {
            Id = Guid.NewGuid(),
            TeacherId = teacherId,
            BaseSalary = baseSalary,
            LessonsRate = lessonsRate,
            EffectiveFrom = effectiveFrom,
            CreatedAt = DateTime.UtcNow
        };
    }
}
