using Courses.Application.Abstractions;
using Courses.Contracts;

namespace Courses.Application;

internal sealed class CourseLookupService(ICourseRepository repository) : ICourseLookup
{
    public async Task<CourseLookupResult?> GetByIdAsync(Guid courseId, CancellationToken ct = default)
    {
        var course = await repository.GetByIdAsync(courseId, ct);

        if (course is null)
            return null;

        return new CourseLookupResult(
            course.Id,
            course.Name,
            course.Price,
            course.DurationMonths
        );
    }
}
