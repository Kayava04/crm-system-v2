using Scheduling.Domain.Entities;
using Scheduling.Domain.Enums;

namespace Crm.UnitTests.Domain;

public class ScheduleTests
{
    private static Schedule NewLesson(DateTime? date = null) =>
        Schedule.CreateForEnrollment(Guid.NewGuid(), Guid.NewGuid(), date ?? new DateTime(2030, 5, 6, 15, 0, 0, DateTimeKind.Utc), 60);

    [Fact]
    public void Create_starts_scheduled_without_cancellation_reason()
    {
        var lesson = NewLesson();

        Assert.Equal(ScheduleStatus.Scheduled, lesson.Status);
        Assert.Equal(CancellationReason.None, lesson.CancellationReason);
        Assert.True(lesson.IsOpen);
    }

    [Fact]
    public void Lesson_belongs_either_to_enrollment_or_group()
    {
        var individual = Schedule.CreateForEnrollment(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddDays(1), 60);
        var group = Schedule.CreateForGroup(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddDays(1), 60);

        Assert.NotNull(individual.EnrollmentId);
        Assert.Null(individual.GroupId);
        Assert.Null(group.EnrollmentId);
        Assert.NotNull(group.GroupId);
    }

    [Fact]
    public void EndDate_is_start_plus_duration()
    {
        var lesson = Schedule.CreateForEnrollment(Guid.NewGuid(), Guid.NewGuid(),
            new DateTime(2030, 5, 6, 15, 0, 0, DateTimeKind.Utc), 90);

        Assert.Equal(new DateTime(2030, 5, 6, 16, 30, 0, DateTimeKind.Utc), lesson.EndDate);
    }

    [Fact]
    public void Unspecified_dates_are_treated_as_utc()
    {
        var lesson = NewLesson(new DateTime(2030, 5, 6, 15, 0, 0, DateTimeKind.Unspecified));

        Assert.Equal(DateTimeKind.Utc, lesson.ScheduledDate.Kind);
        Assert.Equal(15, lesson.ScheduledDate.Hour);
    }

    [Fact]
    public void Local_dates_are_converted_to_utc()
    {
        var local = new DateTime(2030, 5, 6, 15, 0, 0, DateTimeKind.Local);
        var lesson = NewLesson(local);

        Assert.Equal(DateTimeKind.Utc, lesson.ScheduledDate.Kind);
        Assert.Equal(local.ToUniversalTime(), lesson.ScheduledDate);
    }

    [Fact]
    public void Reschedule_moves_the_lesson_and_keeps_it_open()
    {
        var lesson = NewLesson();
        var newDate = new DateTime(2030, 6, 1, 10, 0, 0, DateTimeKind.Utc);

        lesson.Reschedule(newDate);

        Assert.Equal(newDate, lesson.ScheduledDate);
        Assert.Equal(ScheduleStatus.Rescheduled, lesson.Status);
        Assert.True(lesson.IsOpen);
    }

    [Fact]
    public void Completed_and_cancelled_lessons_are_not_open()
    {
        var completed = NewLesson();
        completed.Complete();
        var cancelled = NewLesson();
        cancelled.Cancel();

        Assert.False(completed.IsOpen);
        Assert.False(cancelled.IsOpen);
    }

    [Fact]
    public void Manual_cancel_is_the_default_and_is_never_restored_automatically()
    {
        var lesson = NewLesson();
        lesson.Cancel();

        Assert.Equal(CancellationReason.Manual, lesson.CancellationReason);
        Assert.False(lesson.RestoreIfSystemCancelled());
        Assert.Equal(ScheduleStatus.Cancelled, lesson.Status);
    }

    [Theory]
    [InlineData(CancellationReason.TeacherUnavailable)]
    [InlineData(CancellationReason.EnrollmentInactive)]
    public void System_cancellations_can_be_undone(CancellationReason reason)
    {
        var lesson = NewLesson();
        lesson.Cancel(reason);

        Assert.True(lesson.RestoreIfSystemCancelled());
        Assert.Equal(ScheduleStatus.Scheduled, lesson.Status);
        Assert.Equal(CancellationReason.None, lesson.CancellationReason);
    }

    [Fact]
    public void A_lesson_that_is_not_cancelled_is_not_restored()
    {
        Assert.False(NewLesson().RestoreIfSystemCancelled());
    }

    [Fact]
    public void A_completed_lesson_is_not_restored()
    {
        var lesson = NewLesson();
        lesson.Complete();

        Assert.False(lesson.RestoreIfSystemCancelled());
        Assert.Equal(ScheduleStatus.Completed, lesson.Status);
    }

    [Fact]
    public void ReassignTeacher_changes_only_the_teacher()
    {
        var lesson = NewLesson();
        var date = lesson.ScheduledDate;
        var newTeacher = Guid.NewGuid();

        lesson.ReassignTeacher(newTeacher);

        Assert.Equal(newTeacher, lesson.TeacherId);
        Assert.Equal(date, lesson.ScheduledDate);
        Assert.Equal(ScheduleStatus.Scheduled, lesson.Status);
    }
}
