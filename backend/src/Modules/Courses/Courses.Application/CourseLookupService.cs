using Courses.Application.Abstractions;
using Courses.Contracts;
using Courses.Domain.Entities;
using Education.Contracts.Enums;

namespace Courses.Application;

internal sealed class CourseLookupService(ICourseRepository repository) : ICourseLookup
{
    public async Task<CourseLookupResult?> GetByIdAsync(Guid courseId, CancellationToken ct = default)
    {
        var course = await repository.GetByIdAsync(courseId, ct);

        return course is null ? null : Map(course);
    }

    public async Task<IReadOnlyList<CourseLookupResult>> GetByIdsAsync(
        IReadOnlyCollection<Guid> courseIds,
        CancellationToken ct = default)
    {
        var courses = await repository.GetByIdsAsync(courseIds, ct);

        return courses.Select(Map).ToList();
    }

    private static CourseLookupResult Map(Course course) => new(
        course.Id,
        course.Name,
        course.Price,
        course.DurationMonths,
        course.LessonsCount,
        course.LessonsPerWeek,
        course.LessonType == LessonType.Group,
        course.Format == Format.Online
    );
}
