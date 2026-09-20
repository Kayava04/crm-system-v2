namespace Courses.Contracts;

public interface ICourseLookup
{
    Task<CourseLookupResult?> GetByIdAsync(
        Guid courseId,
        CancellationToken ct = default
    );

    Task<IReadOnlyList<CourseLookupResult>> GetByIdsAsync(
        IReadOnlyCollection<Guid> courseIds,
        CancellationToken ct = default
    );
}

public sealed record CourseLookupResult(
    Guid Id,
    string Name,
    decimal Price,
    int DurationMonths,
    int LessonsCount,
    int LessonsPerWeek,
    bool IsGroup,
    bool IsOnline
);
