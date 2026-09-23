using Students.Application.Abstractions;
using Students.Contracts;
using Students.Domain.Enums;

namespace Students.Application;

internal sealed class StudentStatisticsService(IStudentRepository repository) : IStudentStatistics
{
    public async Task<StudentSummaryResult> GetSummaryAsync(CancellationToken ct = default)
    {
        var counts = await repository.GetCountsByStatusAsync(ct);

        var byStatus = Enum.GetValues<StudentStatus>()
            .ToDictionary(s => s.ToString(), s => counts.GetValueOrDefault(s));

        return new StudentSummaryResult(byStatus.Values.Sum(), byStatus);
    }
}
