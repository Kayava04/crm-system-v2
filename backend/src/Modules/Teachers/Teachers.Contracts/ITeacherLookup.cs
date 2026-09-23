namespace Teachers.Contracts;

public interface ITeacherLookup
{
    Task<TeacherSalaryLookupResult?> GetCurrentSalaryAsync(
        Guid teacherId,
        CancellationToken ct = default
    );

    Task<TeacherProfileResult?> GetByUserIdAsync(
        Guid userId,
        CancellationToken ct = default
    );

    Task<IReadOnlyList<TeacherProfileResult>> GetByIdsAsync(
        IReadOnlyCollection<Guid> teacherIds,
        CancellationToken ct = default
    );
}

public sealed record TeacherProfileResult(
    Guid Id,
    string FullName,
    Guid? UserId
);

public sealed record TeacherSalaryLookupResult(
    Guid TeacherId,
    decimal BaseSalary,
    decimal LessonsRate
);
