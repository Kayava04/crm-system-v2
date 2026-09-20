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

// Calculated over teachers that currently work (not Resigned / Dismissed) and have a salary rate in effect
public sealed record TeacherSalaryOverview(
    int TeachersWithRate,
    decimal TotalBaseSalary,
    decimal AverageBaseSalary,
    decimal AverageLessonsRate
);
