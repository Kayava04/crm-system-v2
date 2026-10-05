namespace Scheduling.Contracts;

public interface IScheduleLookup
{
    Task<int> GetCompletedLessonsCountAsync(
        Guid teacherId,
        DateTime from,
        DateTime to,
        CancellationToken ct = default
    );

    Task<UpcomingLessonsResult> GetUpcomingLessonsAsync(DateTime from, DateTime to, CancellationToken ct = default);

    Task<bool> HasTeacherHistoryAsync(Guid teacherId, CancellationToken ct = default);

    Task<IReadOnlyList<Guid>> GetEnrollmentIdsByTeacherAsync(
        Guid teacherId,
        CancellationToken ct = default
    );
}

public sealed record UpcomingLessonsResult(string TimeZoneId, IReadOnlyList<UpcomingLesson> Lessons);

public sealed record UpcomingLesson(
    Guid LessonId,
    DateTime StartsAt,
    int DurationMinutes,
    Guid TeacherId,
    string? GroupName,
    IReadOnlyList<Guid> EnrollmentIds
);
