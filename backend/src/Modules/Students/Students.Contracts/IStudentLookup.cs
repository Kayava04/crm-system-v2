namespace Students.Contracts;

public interface IStudentLookup
{
    Task<IReadOnlyList<StudentLookupResult>> GetByTeacherAsync(
        Guid teacherId,
        CancellationToken ct = default
    );
}

public sealed record StudentLookupResult(
    Guid Id,
    string FullName,
    string Email,
    string PhoneNumber
);
