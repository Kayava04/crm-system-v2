namespace Courses.Contracts;

public interface ICourseLookup
{
    Task<CourseLookupResult?> GetByIdAsync(
        Guid courseId,
        CancellationToken ct = default
    );
}

public sealed record CourseLookupResult(
    Guid Id,
    string Name,
    decimal Price,
    int DurationMonths
);
