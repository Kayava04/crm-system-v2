using Teachers.Application.Abstractions;
using Teachers.Contracts;

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
}
