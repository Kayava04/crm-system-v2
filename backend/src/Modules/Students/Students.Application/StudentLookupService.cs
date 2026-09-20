using Enrollments.Contracts;
using Scheduling.Contracts;
using Students.Application.Abstractions;
using Students.Contracts;
using Students.Domain.Entities;

namespace Students.Application;

internal sealed class StudentLookupService(
    IStudentRepository repository,
    IScheduleLookup scheduleLookup,
    IEnrollmentLookup enrollmentLookup
) : IStudentLookup
{
    public async Task<IReadOnlyList<StudentLookupResult>> GetByTeacherAsync(
        Guid teacherId,
        CancellationToken ct = default)
    {
        var enrollmentIds = await scheduleLookup.GetEnrollmentIdsByTeacherAsync(teacherId, ct);
        if (enrollmentIds.Count == 0)
            return [];

        var studentIds = await enrollmentLookup.GetStudentIdsAsync(enrollmentIds.ToList(), ct);
        if (studentIds.Count == 0)
            return [];

        var students = await repository.GetByIdsAsync(studentIds.ToList(), ct);

        return students.Select(Map).ToList();
    }

    public async Task<StudentLookupResult?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        var student = await repository.GetByUserIdAsync(userId, ct);

        return student is null ? null : Map(student);
    }

    public async Task<IReadOnlyList<StudentLookupResult>> GetByIdsAsync(
        IReadOnlyCollection<Guid> studentIds,
        CancellationToken ct = default)
    {
        if (studentIds.Count == 0)
            return [];

        var students = await repository.GetByIdsAsync(studentIds, ct);

        return students.Select(Map).ToList();
    }

    private static StudentLookupResult Map(Student s) => new(
        s.Id,
        string.Join(' ', new[] { s.LastName, s.FirstName, s.MiddleName }
            .Where(x => !string.IsNullOrWhiteSpace(x))),
        s.Email,
        s.PhoneNumber);
}
