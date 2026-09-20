using Enrollments.Contracts;
using FluentValidation;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Notifications.Application.Services;
using Notifications.Contracts;
using Scheduling.Contracts;
using Students.Contracts;
using Teachers.Contracts;

namespace Notifications.Application.Features.SendLessonReminders;

public sealed record SendLessonRemindersRequest(int HoursAhead = 24);

public sealed record SendLessonRemindersResponse(
    int LessonsFound,
    int Created,
    int SkippedAlreadySent
);

public sealed class SendLessonRemindersValidator : AbstractValidator<SendLessonRemindersRequest>
{
    public SendLessonRemindersValidator()
    {
        RuleFor(x => x.HoursAhead)
            .InclusiveBetween(1, 168).WithMessage("Hours ahead must be between 1 and 168.");
    }
}

public static class SendLessonRemindersEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/lesson-reminders", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanManageNotifications))
             .WithName("SendLessonReminders")
             .WithSummary("Remind students and teachers about lessons that start soon (each reminder is sent once)")
             .Produces<SendLessonRemindersResponse>(StatusCodes.Status200OK)
             .ProducesValidationProblem();
    }

    private static async Task<IResult> Handle(
        SendLessonRemindersRequest request,
        IValidator<SendLessonRemindersRequest> validator,
        IScheduleLookup scheduleLookup,
        IEnrollmentLookup enrollmentLookup,
        IStudentLookup studentLookup,
        ITeacherLookup teacherLookup,
        NotificationDispatcher dispatcher,
        ILogger<SendLessonRemindersRequest> logger,
        CancellationToken ct
    )
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
            return Results.ValidationProblem(validationResult.ToDictionary());

        var now = DateTime.UtcNow;
        var upcoming = await scheduleLookup.GetUpcomingLessonsAsync(now, now.AddHours(request.HoursAhead), ct);

        if (upcoming.Lessons.Count == 0)
            return Results.Ok(new SendLessonRemindersResponse(0, 0, 0));

        // Only enrollments that are still active attend; a paused student gets no reminder
        var enrollments = (await enrollmentLookup.GetByIdsAsync(
                upcoming.Lessons.SelectMany(l => l.EnrollmentIds).Distinct().ToList(), ct))
            .Where(e => e.IsActive)
            .ToDictionary(e => e.Id);

        var students = (await studentLookup.GetByIdsAsync(
                enrollments.Values.Select(e => e.StudentId).Distinct().ToList(), ct))
            .ToDictionary(s => s.Id);

        var teachers = (await teacherLookup.GetByIdsAsync(
                upcoming.Lessons.Select(l => l.TeacherId).Distinct().ToList(), ct))
            .ToDictionary(t => t.Id);

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(upcoming.TimeZoneId);
        var drafts = new List<NotificationDraft>();

        foreach (var lesson in upcoming.Lessons)
        {
            var local = TimeZoneInfo.ConvertTimeFromUtc(lesson.StartsAt, timeZone);
            var when = $"{local:yyyy-MM-dd HH:mm} ({upcoming.TimeZoneId})";
            var what = lesson.GroupName is null ? "Your lesson" : $"Your group lesson ({lesson.GroupName})";

            // The key contains the start time, so a rescheduled lesson gets a fresh reminder
            var key = $"lesson:{lesson.LessonId}:{lesson.StartsAt:yyyyMMddHHmm}";

            var studentUserIds = lesson.EnrollmentIds
                .Where(enrollments.ContainsKey)
                .Select(e => students.GetValueOrDefault(enrollments[e].StudentId)?.UserId)
                .OfType<Guid>();

            foreach (var userId in studentUserIds)
                drafts.Add(new NotificationDraft(userId, "Lesson reminder", $"{what} starts on {when}.", key));

            if (teachers.GetValueOrDefault(lesson.TeacherId)?.UserId is { } teacherUserId)
                drafts.Add(new NotificationDraft(
                    teacherUserId, "Lesson reminder",
                    $"You have {(lesson.GroupName is null ? "a lesson" : $"a group lesson ({lesson.GroupName})")} on {when}.", key));
        }

        var result = await dispatcher.DispatchAsync(NotificationType.LessonReminder, drafts, ct: ct);

        logger.LogInformation(
            "Lesson reminders: {Lessons} lessons, {Created} sent, {Duplicates} already sent",
            upcoming.Lessons.Count, result.Created, result.Duplicates);

        return Results.Ok(new SendLessonRemindersResponse(upcoming.Lessons.Count, result.Created, result.Duplicates));
    }
}
