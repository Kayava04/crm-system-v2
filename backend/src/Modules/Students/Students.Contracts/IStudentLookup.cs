namespace Students.Contracts;

public interface IStudentLookup
{
    Task<IReadOnlyList<StudentLookupResult>> GetByTeacherAsync(
        Guid teacherId,
        CancellationToken ct = default
    );

    Task<StudentLookupResult?> GetByUserIdAsync(
        Guid userId,
        CancellationToken ct = default
    );

    Task<IReadOnlyList<StudentLookupResult>> GetByIdsAsync(
        IReadOnlyCollection<Guid> studentIds,
        CancellationToken ct = default
    );
}

public sealed record StudentLookupResult(
    Guid Id,
    string FullName,
    string Email,
    string PhoneNumber,
    Guid? UserId
);
