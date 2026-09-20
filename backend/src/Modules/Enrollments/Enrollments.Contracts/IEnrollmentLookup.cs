namespace Enrollments.Contracts;

public interface IEnrollmentLookup
{
    Task<EnrollmentLookupResult?> GetByIdAsync(
        Guid enrollmentId,
        CancellationToken ct = default
    );

    Task<IReadOnlyList<EnrollmentLookupResult>> GetByIdsAsync(
        IReadOnlyCollection<Guid> enrollmentIds,
        CancellationToken ct = default
    );

    Task<IReadOnlyList<EnrollmentLookupResult>> GetByStudentAsync(
        Guid studentId,
        CancellationToken ct = default
    );

    // Of the given students, those that have at least one enrollment
    Task<IReadOnlySet<Guid>> GetStudentIdsWithEnrollmentsAsync(
        IReadOnlyCollection<Guid> studentIds,
        CancellationToken ct = default
    );

    Task<IReadOnlyList<Guid>> GetIdsByStudentAsync(
        Guid studentId,
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
    decimal EffectivePrice,
    DateOnly StartDate,
    DateOnly EndDate
);
