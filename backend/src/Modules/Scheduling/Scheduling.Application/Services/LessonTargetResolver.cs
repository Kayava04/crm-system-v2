using Courses.Contracts;
using Enrollments.Contracts;
using Microsoft.AspNetCore.Http;
using Scheduling.Application.Abstractions;

namespace Scheduling.Application.Services;

// What the lessons are scheduled for: one enrollment (individual course) or one study group (group course)
internal sealed record LessonTarget(
    Guid? EnrollmentId,
    Guid? GroupId,
    Guid? DefaultTeacherId,
    DateOnly? EnrollmentStartDate,
    CourseLookupResult Course
);

internal sealed record LessonTargetResolution(LessonTarget? Target, IResult? Error);

internal sealed class LessonTargetResolver(
    IEnrollmentLookup enrollmentLookup,
    ICourseLookup courseLookup,
    IStudyGroupRepository groupRepository
)
{
    public async Task<LessonTargetResolution> ResolveAsync(
        Guid? enrollmentId,
        Guid? groupId,
        CancellationToken ct)
    {
        if (enrollmentId is not null)
        {
            var enrollment = await enrollmentLookup.GetByIdAsync(enrollmentId.Value, ct);
            if (enrollment is null)
                return Fail($"Enrollment with id '{enrollmentId}' not found.", StatusCodes.Status404NotFound);

            if (!enrollment.IsActive)
                return Fail("Lessons can only be scheduled for an active enrollment.", StatusCodes.Status409Conflict);

            var course = await courseLookup.GetByIdAsync(enrollment.CourseId, ct);
            if (course is null)
                return Fail($"Course with id '{enrollment.CourseId}' not found.", StatusCodes.Status404NotFound);

            if (course.IsGroup)
                return Fail(
                    "The course of this enrollment is a group course. Schedule its lessons through a study group.",
                    StatusCodes.Status409Conflict);

            return new LessonTargetResolution(
                new LessonTarget(enrollmentId, null, null, enrollment.StartDate, course), null);
        }

        var group = await groupRepository.GetByIdAsync(groupId!.Value, ct);
        if (group is null)
            return Fail($"Study group with id '{groupId}' not found.", StatusCodes.Status404NotFound);

        var groupCourse = await courseLookup.GetByIdAsync(group.CourseId, ct);
        if (groupCourse is null)
            return Fail($"Course with id '{group.CourseId}' not found.", StatusCodes.Status404NotFound);

        return new LessonTargetResolution(
            new LessonTarget(null, groupId, group.TeacherId, null, groupCourse), null);
    }

    private static LessonTargetResolution Fail(string detail, int statusCode) =>
        new(null, Results.Problem(detail: detail, statusCode: statusCode));
}
