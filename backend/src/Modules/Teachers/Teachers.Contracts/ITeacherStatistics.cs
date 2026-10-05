namespace Teachers.Contracts;

public interface ITeacherStatistics
{
    Task<TeacherSummaryResult> GetSummaryAsync(CancellationToken ct = default);
}

public sealed record TeacherSummaryResult(
    int Total,
    IReadOnlyDictionary<string, int> ByStatus,
    TeacherSalaryOverview SalaryOverview
);

public sealed record TeacherSalaryOverview(
    int TeachersWithRate,
    decimal TotalBaseSalary,
    decimal AverageBaseSalary,
    decimal AverageLessonsRate
);
