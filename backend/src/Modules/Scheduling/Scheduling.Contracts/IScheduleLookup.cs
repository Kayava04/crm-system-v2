namespace Scheduling.Contracts;

public interface IScheduleLookup
{
    Task<int> GetCompletedLessonsCountAsync(
        Guid teacherId,
        DateTime from,
        DateTime to,
        CancellationToken ct = default
    );

    // True if the teacher has ever had a lesson or a group; such a teacher must be deactivated, not deleted
    Task<bool> HasTeacherHistoryAsync(Guid teacherId, CancellationToken ct = default);

    Task<IReadOnlyList<Guid>> GetEnrollmentIdsByTeacherAsync(
        Guid teacherId,
        CancellationToken ct = default
    );
}
