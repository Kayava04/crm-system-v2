using Enrollments.Application.Abstractions;
using Enrollments.Contracts;
using Enrollments.Domain.Enums;

namespace Enrollments.Application;

internal sealed class EnrollmentLookupService(IEnrollmentRepository repository) : IEnrollmentLookup
{
    public async Task<EnrollmentLookupResult?> GetByIdAsync(Guid enrollmentId, CancellationToken ct = default)
    {
        var enrollment = await repository.GetByIdAsync(enrollmentId, ct);

        if (enrollment is null)
            return null;

        return new EnrollmentLookupResult(
            enrollment.Id,
            enrollment.StudentId,
            enrollment.CourseId,
            enrollment.Status == EnrollmentStatus.Active
        );
    }

    public async Task<IReadOnlyList<Guid>> GetStudentIdsAsync(
        IReadOnlyCollection<Guid> enrollmentIds,
        CancellationToken ct = default) =>
        await repository.GetStudentIdsByEnrollmentIdsAsync(enrollmentIds, ct);
}
