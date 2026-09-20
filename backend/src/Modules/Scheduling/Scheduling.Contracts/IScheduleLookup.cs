namespace Scheduling.Contracts;

public interface IScheduleLookup
{
    Task<int> GetCompletedLessonsCountAsync(
        Guid teacherId,
        DateTime from,
        DateTime to,
        CancellationToken ct = default
    );

    Task<IReadOnlyList<Guid>> GetEnrollmentIdsByTeacherAsync(
        Guid teacherId,
        CancellationToken ct = default
    );
}
