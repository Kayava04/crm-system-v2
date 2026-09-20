namespace Scheduling.Contracts;

public interface IScheduleLookup
{
    Task<int> GetCompletedLessonsCountAsync(
        Guid teacherId,
        DateTime from,
        DateTime to,
        CancellationToken ct = default
    );

    // Lessons that are still going to be held between the two moments, with everyone who attends them
    Task<UpcomingLessonsResult> GetUpcomingLessonsAsync(DateTime from, DateTime to, CancellationToken ct = default);

    // True if the teacher has ever had a lesson or a group; such a teacher must be deactivated, not deleted
    Task<bool> HasTeacherHistoryAsync(Guid teacherId, CancellationToken ct = default);

    Task<IReadOnlyList<Guid>> GetEnrollmentIdsByTeacherAsync(
        Guid teacherId,
        CancellationToken ct = default
    );
}

public sealed record UpcomingLessonsResult(string TimeZoneId, IReadOnlyList<UpcomingLesson> Lessons);

// EnrollmentIds are the attending enrollments: one for an individual lesson, the group's members for a group lesson
public sealed record UpcomingLesson(
    Guid LessonId,
    DateTime StartsAt,
    int DurationMinutes,
    Guid TeacherId,
    string? GroupName,
    IReadOnlyList<Guid> EnrollmentIds
);
