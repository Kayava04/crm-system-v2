using Enrollments.Application.Abstractions;
using Enrollments.Contracts;
using Enrollments.Domain.Entities;
using Enrollments.Domain.Enums;

namespace Enrollments.Application;

internal sealed class EnrollmentLookupService(IEnrollmentRepository repository) : IEnrollmentLookup
{
    public async Task<EnrollmentLookupResult?> GetByIdAsync(Guid enrollmentId, CancellationToken ct = default)
    {
        var enrollment = await repository.GetByIdAsync(enrollmentId, ct);

        return enrollment is null ? null : Map(enrollment);
    }

    public async Task<IReadOnlyList<EnrollmentLookupResult>> GetByIdsAsync(
        IReadOnlyCollection<Guid> enrollmentIds,
        CancellationToken ct = default)
    {
        var enrollments = await repository.GetByIdsAsync(enrollmentIds, ct);

        return enrollments.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<EnrollmentLookupResult>> GetByStudentAsync(
        Guid studentId,
        CancellationToken ct = default)
    {
        var enrollments = await repository.GetByStudentAsync(studentId, ct);

        return enrollments.Select(Map).ToList();
    }

    public async Task<IReadOnlySet<Guid>> GetStudentIdsWithEnrollmentsAsync(
        IReadOnlyCollection<Guid> studentIds,
        CancellationToken ct = default) =>
        await repository.GetStudentIdsWithEnrollmentsAsync(studentIds, ct);

    public async Task<IReadOnlyList<Guid>> GetIdsByStudentAsync(Guid studentId, CancellationToken ct = default) =>
        await repository.GetIdsByStudentAsync(studentId, ct);

    public async Task<IReadOnlyList<Guid>> GetStudentIdsAsync(
        IReadOnlyCollection<Guid> enrollmentIds,
        CancellationToken ct = default) =>
        await repository.GetStudentIdsByEnrollmentIdsAsync(enrollmentIds, ct);

    private static EnrollmentLookupResult Map(Enrollment enrollment) => new(
        enrollment.Id,
        enrollment.StudentId,
        enrollment.CourseId,
        enrollment.Status == EnrollmentStatus.Active,
        enrollment.EffectivePrice,
        enrollment.StartDate,
        enrollment.EndDate
    );
}
