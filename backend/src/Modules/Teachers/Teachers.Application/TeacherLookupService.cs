using Teachers.Application.Abstractions;
using Teachers.Contracts;
using Teachers.Domain.Entities;

namespace Teachers.Application;

internal sealed class TeacherLookupService(ITeacherRepository repository) : ITeacherLookup
{
    public async Task<TeacherSalaryLookupResult?> GetCurrentSalaryAsync(
        Guid teacherId,
        CancellationToken ct = default)
    {
        var teacher = await repository.GetByIdAsync(teacherId, ct);

        var rate = teacher?.CurrentSalaryRate;
        if (rate is null)
            return null;

        return new TeacherSalaryLookupResult(teacher!.Id, rate.BaseSalary, rate.LessonsRate);
    }

    public async Task<TeacherProfileResult?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        var teacher = await repository.GetByUserIdAsync(userId, ct);

        return teacher is null ? null : Map(teacher);
    }

    public async Task<IReadOnlyList<TeacherProfileResult>> GetByIdsAsync(
        IReadOnlyCollection<Guid> teacherIds,
        CancellationToken ct = default)
    {
        if (teacherIds.Count == 0)
            return [];

        var teachers = await repository.GetByIdsAsync(teacherIds, ct);

        return teachers.Select(Map).ToList();
    }

    private static TeacherProfileResult Map(Teacher t) => new(
        t.Id,
        string.Join(' ', new[] { t.LastName, t.FirstName, t.MiddleName }
            .Where(x => !string.IsNullOrWhiteSpace(x))),
        t.UserId);
}
