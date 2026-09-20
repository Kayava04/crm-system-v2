using Identity.Domain.Entities;
using Notifications.Contracts;
using Notifications.Domain.Entities;
using Teachers.Domain.Entities;
using Teachers.Domain.Enums;

namespace Crm.UnitTests.Domain;

public class TeacherAndUserTests
{
    private static Teacher NewTeacher() => Teacher.Create(
        "Anna", "Koval", null, new DateOnly(1985, 5, 5), "+380501112233", "anna@example.test", "Kyiv", "Ukraine");

    [Fact]
    public void New_teacher_is_on_probation_without_account()
    {
        var teacher = NewTeacher();

        Assert.Equal(TeacherStatus.Probation, teacher.Status);
        Assert.Null(teacher.UserId);
    }

    [Fact]
    public void Current_salary_rate_is_the_latest_one_already_in_effect()
    {
        var teacher = NewTeacher();
        teacher.AddSalaryRate(TeacherSalaryRate.Create(teacher.Id, 1000, 100, DateTime.UtcNow.AddDays(-60)));
        teacher.AddSalaryRate(TeacherSalaryRate.Create(teacher.Id, 1200, 120, DateTime.UtcNow.AddDays(-10)));
        teacher.AddSalaryRate(TeacherSalaryRate.Create(teacher.Id, 9999, 999, DateTime.UtcNow.AddDays(30)));   // future

        Assert.Equal(1200, teacher.CurrentSalaryRate!.BaseSalary);
    }

    [Fact]
    public void Teacher_without_a_rate_in_effect_has_no_current_rate()
    {
        var teacher = NewTeacher();
        teacher.AddSalaryRate(TeacherSalaryRate.Create(teacher.Id, 1000, 100, DateTime.UtcNow.AddDays(5)));

        Assert.Null(teacher.CurrentSalaryRate);
    }

    [Fact]
    public void LinkUserAccount_stores_the_user_id()
    {
        var teacher = NewTeacher();
        var userId = Guid.NewGuid();

        teacher.LinkUserAccount(userId);

        Assert.Equal(userId, teacher.UserId);
    }

    [Fact]
    public void New_user_is_active_and_can_be_deactivated_and_activated()
    {
        var user = User.Create("a@example.test");
        Assert.True(user.IsActive);

        user.SetActive(false);
        Assert.False(user.IsActive);

        user.SetActive(true);
        Assert.True(user.IsActive);
    }

    [Fact]
    public void User_password_flags()
    {
        var user = User.Create("a@example.test", mustChangePassword: false);
        Assert.False(user.MustChangePassword);

        user.RequirePasswordChange();
        Assert.True(user.MustChangePassword);

        user.CompletePasswordChange();
        Assert.False(user.MustChangePassword);
    }

    [Fact]
    public void Notification_is_unread_until_read_and_keeps_the_first_read_time()
    {
        var notification = Notification.Create(Guid.NewGuid(), NotificationType.General, "s", "b");
        Assert.False(notification.IsRead);

        notification.MarkRead();
        var firstRead = notification.ReadAt;
        Assert.True(notification.IsRead);

        notification.MarkRead();
        Assert.Equal(firstRead, notification.ReadAt);
    }

    [Fact]
    public void Notification_keeps_its_reference_key_and_action()
    {
        var notification = Notification.Create(Guid.NewGuid(), NotificationType.LessonReminder, "s", "b",
            NotificationAction.None, "lesson:1");

        Assert.Equal("lesson:1", notification.ReferenceKey);
        Assert.Equal(NotificationType.LessonReminder, notification.Type);
    }
}
