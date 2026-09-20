namespace Enrollments.Contracts;

public interface IEnrollmentLookup
{
    Task<EnrollmentLookupResult?> GetByIdAsync(
        Guid enrollmentId,
        CancellationToken ct = default
    );

    Task<IReadOnlyList<Guid>> GetStudentIdsAsync(
        IReadOnlyCollection<Guid> enrollmentIds,
        CancellationToken ct = default
    );
}

public sealed record EnrollmentLookupResult(
    Guid Id,
    Guid StudentId,
    Guid CourseId,
    bool IsActive,
    decimal EffectivePrice
);
