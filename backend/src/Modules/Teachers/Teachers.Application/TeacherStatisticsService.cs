using Teachers.Application.Abstractions;
using Teachers.Contracts;
using Teachers.Domain.Enums;

namespace Teachers.Application;

internal sealed class TeacherStatisticsService(ITeacherRepository repository) : ITeacherStatistics
{
    public async Task<TeacherSummaryResult> GetSummaryAsync(CancellationToken ct = default)
    {
        var teachers = await repository.GetAllWithSalaryRatesAsync(ct);

        var byStatus = Enum.GetValues<TeacherStatus>()
            .ToDictionary(s => s.ToString(), s => teachers.Count(t => t.Status == s));

        var rates = teachers
            .Where(t => t.Status is not (TeacherStatus.Resigned or TeacherStatus.Dismissed))
            .Select(t => t.CurrentSalaryRate)
            .OfType<Domain.Entities.TeacherSalaryRate>()
            .ToList();

        var overview = rates.Count == 0
            ? new TeacherSalaryOverview(0, 0m, 0m, 0m)
            : new TeacherSalaryOverview(
                rates.Count,
                rates.Sum(r => r.BaseSalary),
                Math.Round(rates.Average(r => r.BaseSalary), 2),
                Math.Round(rates.Average(r => r.LessonsRate), 2));

        return new TeacherSummaryResult(teachers.Count, byStatus, overview);
    }
}
