using System.Security.Claims;
using Courses.Contracts;
using Enrollments.Contracts;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Scheduling.Application.Abstractions;
using Scheduling.Application.Services;
using Scheduling.Domain.Enums;
using Students.Contracts;
using Teachers.Contracts;

namespace Scheduling.Application.Features.GetMyCalendar;

public sealed record CalendarItemResponse(
    Guid Id,
    DateTime StartsAt,
    DateTime EndsAt,
    ScheduleStatus Status,
    string CourseName,
    bool IsOnline,
    bool IsGroup,
    string? GroupName,
    string? TeacherName,
    IReadOnlyList<string> Students,
    string? Notes
);

public sealed record CalendarResponse(
    DateOnly From,
    DateOnly To,
    string TimeZone,
    IReadOnlyList<CalendarItemResponse> Items
);

public static class GetMyCalendarEndpoint
{
    private const int MaxRangeDays = 366;

    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/my", Handle)
             .RequireAuthorization()
             .WithName("GetMyCalendar")
             .WithSummary("Get the calendar of the current teacher or student (defaults to the next 30 days)")
             .Produces<CalendarResponse>(StatusCodes.Status200OK)
             .ProducesValidationProblem()
             .ProducesProblem(StatusCodes.Status401Unauthorized)
             .ProducesProblem(StatusCodes.Status403Forbidden)
             .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        ClaimsPrincipal user,
        IScheduleRepository scheduleRepository,
        IStudyGroupRepository groupRepository,
        IEnrollmentLookup enrollmentLookup,
        ICourseLookup courseLookup,
        ITeacherLookup teacherLookup,
        IStudentLookup studentLookup,
        ISchoolClock clock,
        CancellationToken ct,
        DateOnly? from = null,
        DateOnly? to = null
    )
    {
        var fromDate = from ?? clock.Today;
        var toDate = to ?? fromDate.AddDays(30);

        if (toDate < fromDate || toDate.DayNumber - fromDate.DayNumber > MaxRangeDays)
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["to"] = [$"Date range must be positive and not exceed {MaxRangeDays} days."]
            });

        // JwtBearer maps the "sub" claim to NameIdentifier by default
        var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Results.Problem(
                detail: "Invalid user identity.",
                statusCode: StatusCodes.Status401Unauthorized
            );

        // Both bounds are inclusive days in school time
        var fromUtc = clock.ToUtc(fromDate, TimeOnly.MinValue);
        var toUtc = clock.ToUtc(toDate.AddDays(1), TimeOnly.MinValue);

        var isTeacher = user.IsInRole(nameof(SystemRole.Teacher));
        var isStudent = user.IsInRole(nameof(SystemRole.Student));

        IReadOnlyList<Domain.Entities.Schedule> lessons;

        if (isTeacher)
        {
            var teacher = await teacherLookup.GetByUserIdAsync(userId, ct);
            if (teacher is null)
                return Results.Problem(
                    detail: "Teacher profile is not linked to this account.",
                    statusCode: StatusCodes.Status404NotFound
                );

            lessons = await scheduleRepository.GetForCalendarAsync(teacher.Id, [], [], fromUtc, toUtc, ct);
        }
        else if (isStudent)
        {
            var student = await studentLookup.GetByUserIdAsync(userId, ct);
            if (student is null)
                return Results.Problem(
                    detail: "Student profile is not linked to this account.",
                    statusCode: StatusCodes.Status404NotFound
                );

            var enrollmentIds = await enrollmentLookup.GetIdsByStudentAsync(student.Id, ct);
            var groupIds = await groupRepository.GetGroupIdsByEnrollmentsAsync(enrollmentIds, ct);

            lessons = await scheduleRepository.GetForCalendarAsync(null, enrollmentIds, groupIds, fromUtc, toUtc, ct);
        }
        else
        {
            return Results.Problem(
                detail: "The calendar is available for teachers and students only.",
                statusCode: StatusCodes.Status403Forbidden
            );
        }

        var items = await BuildItemsAsync(
            lessons, isTeacher, groupRepository, enrollmentLookup, courseLookup, teacherLookup, studentLookup, ct);

        return Results.Ok(new CalendarResponse(fromDate, toDate, clock.TimeZoneId, items));
    }

    private static async Task<IReadOnlyList<CalendarItemResponse>> BuildItemsAsync(
        IReadOnlyList<Domain.Entities.Schedule> lessons,
        bool teacherView,
        IStudyGroupRepository groupRepository,
        IEnrollmentLookup enrollmentLookup,
        ICourseLookup courseLookup,
        ITeacherLookup teacherLookup,
        IStudentLookup studentLookup,
        CancellationToken ct)
    {
        if (lessons.Count == 0)
            return [];

        var groups = (await groupRepository.GetByIdsAsync(
                lessons.Where(l => l.GroupId.HasValue).Select(l => l.GroupId!.Value).Distinct().ToList(), ct))
            .ToDictionary(g => g.Id);

        // Enrollments are needed for individual lessons and, for the teacher, to list the members of group lessons
        var enrollmentIds = lessons
            .Where(l => l.EnrollmentId.HasValue)
            .Select(l => l.EnrollmentId!.Value)
            .ToHashSet();

        if (teacherView)
            foreach (var group in groups.Values)
                enrollmentIds.UnionWith(group.Members.Select(m => m.EnrollmentId));

        var enrollments = (await enrollmentLookup.GetByIdsAsync(enrollmentIds, ct)).ToDictionary(e => e.Id);

        var courseIds = enrollments.Values.Select(e => e.CourseId)
            .Concat(groups.Values.Select(g => g.CourseId))
            .Distinct()
            .ToList();
        var courses = (await courseLookup.GetByIdsAsync(courseIds, ct)).ToDictionary(c => c.Id);

        var studentNames = new Dictionary<Guid, string>();
        var teacherNames = new Dictionary<Guid, string>();

        if (teacherView)
        {
            var studentIds = enrollments.Values.Select(e => e.StudentId).Distinct().ToList();
            studentNames = (await studentLookup.GetByIdsAsync(studentIds, ct)).ToDictionary(s => s.Id, s => s.FullName);
        }
        else
        {
            var teacherIds = lessons.Select(l => l.TeacherId).Distinct().ToList();
            teacherNames = (await teacherLookup.GetByIdsAsync(teacherIds, ct)).ToDictionary(t => t.Id, t => t.FullName);
        }

        return lessons.Select(lesson =>
        {
            Guid? courseId = null;
            string? groupName = null;
            IEnumerable<Guid> memberEnrollmentIds = [];

            if (lesson.GroupId is { } groupId && groups.TryGetValue(groupId, out var group))
            {
                courseId = group.CourseId;
                groupName = group.Name;
                memberEnrollmentIds = group.Members.Select(m => m.EnrollmentId);
            }
            else if (lesson.EnrollmentId is { } enrollmentId && enrollments.TryGetValue(enrollmentId, out var enrollment))
            {
                courseId = enrollment.CourseId;
                memberEnrollmentIds = [enrollmentId];
            }

            var course = courseId is { } id && courses.TryGetValue(id, out var c) ? c : null;

            var students = teacherView
                ? memberEnrollmentIds
                    .Where(enrollments.ContainsKey)
                    .Select(e => studentNames.GetValueOrDefault(enrollments[e].StudentId, "Unknown student"))
                    .Order()
                    .ToList()
                : [];

            return new CalendarItemResponse(
                lesson.Id,
                lesson.ScheduledDate,
                lesson.EndDate,
                lesson.Status,
                course?.Name ?? "Unknown course",
                course?.IsOnline ?? false,
                lesson.GroupId.HasValue,
                groupName,
                teacherView ? null : teacherNames.GetValueOrDefault(lesson.TeacherId),
                students,
                lesson.Notes);
        }).ToList();
    }
}
