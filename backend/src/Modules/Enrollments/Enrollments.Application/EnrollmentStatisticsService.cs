using Enrollments.Application.Abstractions;
using Enrollments.Contracts;
using Enrollments.Domain.Enums;

namespace Enrollments.Application;

internal sealed class EnrollmentStatisticsService(IEnrollmentRepository repository) : IEnrollmentStatistics
{
    public async Task<EnrollmentSummaryResult> GetSummaryAsync(CancellationToken ct = default)
    {
        var counts = await repository.GetCountsByStatusAsync(ct);
        var byCourse = await repository.GetCountsByCourseAsync(ct);

        var byStatus = Enum.GetValues<EnrollmentStatus>()
            .ToDictionary(s => s.ToString(), s => counts.GetValueOrDefault(s));

        return new EnrollmentSummaryResult(
            byStatus.Values.Sum(),
            byStatus,
            byCourse
                .OrderByDescending(x => x.Value)
                .Select(x => new EnrollmentCourseCount(x.Key, x.Value))
                .ToList());
    }
}
