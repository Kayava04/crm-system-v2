using Enrollments.Contracts;
using Microsoft.Extensions.Logging;
using Notifications.Contracts;
using Scheduling.Contracts;
using Students.Contracts;
using Teachers.Contracts;

namespace Notifications.Application.Services;

internal sealed record LessonReminderResult(int LessonsFound, int Created, int SkippedAlreadySent);

// Used by the admin endpoint and by the periodic job, so both always behave the same
internal sealed class LessonReminderService(
    IScheduleLookup scheduleLookup,
    IEnrollmentLookup enrollmentLookup,
    IStudentLookup studentLookup,
    ITeacherLookup teacherLookup,
    NotificationDispatcher dispatcher,
    ILogger<LessonReminderService> logger)
{
    public async Task<LessonReminderResult> SendAsync(int hoursAhead, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var upcoming = await scheduleLookup.GetUpcomingLessonsAsync(now, now.AddHours(hoursAhead), ct);

        if (upcoming.Lessons.Count == 0)
            return new LessonReminderResult(0, 0, 0);

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

        return new LessonReminderResult(upcoming.Lessons.Count, result.Created, result.Duplicates);
    }
}
