namespace Teachers.Contracts;

public interface ITeacherLookup
{
    Task<TeacherSalaryLookupResult?> GetCurrentSalaryAsync(
        Guid teacherId,
        CancellationToken ct = default
    );
}

public sealed record TeacherSalaryLookupResult(
    Guid TeacherId,
    decimal BaseSalary,
    decimal LessonsRate
);
