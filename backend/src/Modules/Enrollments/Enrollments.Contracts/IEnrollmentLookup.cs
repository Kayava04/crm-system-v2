namespace Enrollments.Contracts;

public interface IEnrollmentLookup
{
    Task<EnrollmentLookupResult?> GetByIdAsync(
        Guid enrollmentId,
        CancellationToken ct = default
    );
}

public sealed record EnrollmentLookupResult(
    Guid Id,
    Guid StudentId,
    Guid CourseId,
    bool IsActive
);
