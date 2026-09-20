using Notifications.Application.Features.BroadcastNotification;
using Notifications.Application.Features.SendInvoiceReminders;
using Notifications.Application.Features.SendLessonReminders;
using Notifications.Application.Features.SendNotification;
using Students.Application.Features.BulkCreateStudents;
using Students.Application.Features.BulkDeleteStudents;
using Students.Application.Features.CreateStudent;
using Teachers.Application.Features.BulkCreateTeachers;
using Teachers.Application.Features.BulkDeleteTeachers;

namespace Crm.UnitTests.Validators;

public class BulkAndNotificationValidatorTests
{
    // ---- bulk requests
    [Fact]
    public void Bulk_requests_need_between_1_and_500_items()
    {
        Assert.False(new BulkCreateStudentsValidator().Validate(new BulkCreateStudentsRequest([])).IsValid);
        Assert.False(new BulkCreateTeachersValidator().Validate(new BulkCreateTeachersRequest([])).IsValid);
        Assert.False(new BulkDeleteStudentsValidator().Validate(new BulkDeleteStudentsRequest([])).IsValid);
        Assert.False(new BulkDeleteTeachersValidator().Validate(new BulkDeleteTeachersRequest([])).IsValid);

        var ids = Enumerable.Range(0, 500).Select(_ => Guid.NewGuid()).ToList();
        Assert.True(new BulkDeleteStudentsValidator().Validate(new BulkDeleteStudentsRequest(ids)).IsValid);
        Assert.True(new BulkDeleteTeachersValidator().Validate(new BulkDeleteTeachersRequest(ids)).IsValid);

        ids.Add(Guid.NewGuid());
        Assert.False(new BulkDeleteStudentsValidator().Validate(new BulkDeleteStudentsRequest(ids)).IsValid);
        Assert.False(new BulkDeleteTeachersValidator().Validate(new BulkDeleteTeachersRequest(ids)).IsValid);
    }

    // ---- single student rules that bulk creation reuses for every item
    private static CreateRequest Student(string phone = "+380501112244", DateOnly? birth = null, List<Education.Contracts.Enums.Language>? languages = null, int intensity = 3) =>
        new("Ivan", "Petrenko", null, birth ?? new DateOnly(2000, 1, 1), phone, "ivan@example.test", "Kyiv", "Ukraine", false, null,
            Students.Domain.Enums.LearningGoal.Work, Education.Contracts.Enums.Format.Online, Education.Contracts.Enums.LessonType.Individual,
            intensity, Education.Contracts.Enums.Level.A2, false, languages ?? [Education.Contracts.Enums.Language.English]);

    [Fact]
    public void A_correct_student_is_valid()
    {
        Assert.True(new CreateValidator().Validate(Student()).IsValid);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("12")]
    [InlineData("+38050111224455667788990")]
    public void Student_phone_must_look_like_a_phone(string phone)
    {
        Assert.False(new CreateValidator().Validate(Student(phone: phone)).IsValid);
    }

    [Fact]
    public void Student_cannot_be_born_in_the_future()
    {
        Assert.False(new CreateValidator().Validate(Student(birth: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)))).IsValid);
    }

    [Fact]
    public void Student_languages_must_not_repeat()
    {
        var repeated = new List<Education.Contracts.Enums.Language> { Education.Contracts.Enums.Language.English, Education.Contracts.Enums.Language.English };

        Assert.False(new CreateValidator().Validate(Student(languages: repeated)).IsValid);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(7, true)]
    [InlineData(8, false)]
    public void Student_intensity_is_between_1_and_7(int intensity, bool valid)
    {
        Assert.Equal(valid, new CreateValidator().Validate(Student(intensity: intensity)).IsValid);
    }

    // ---- notifications
    [Fact]
    public void Send_needs_recipients_subject_and_body()
    {
        var validator = new SendNotificationValidator();
        var one = new List<Guid> { Guid.NewGuid() };

        Assert.True(validator.Validate(new SendNotificationRequest(one, "s", "b")).IsValid);
        Assert.False(validator.Validate(new SendNotificationRequest([], "s", "b")).IsValid);
        Assert.False(validator.Validate(new SendNotificationRequest(one, "", "b")).IsValid);
        Assert.False(validator.Validate(new SendNotificationRequest(one, "s", "")).IsValid);
        Assert.False(validator.Validate(new SendNotificationRequest([Guid.Empty], "s", "b")).IsValid);
        Assert.False(validator.Validate(new SendNotificationRequest(one, new string('x', 201), "b")).IsValid);
        Assert.False(validator.Validate(new SendNotificationRequest(one, "s", new string('x', 2001))).IsValid);
        Assert.False(validator.Validate(new SendNotificationRequest(Enumerable.Range(0, 1001).Select(_ => Guid.NewGuid()).ToList(), "s", "b")).IsValid);
    }

    [Fact]
    public void Broadcast_needs_a_known_audience()
    {
        var validator = new BroadcastNotificationValidator();

        Assert.True(validator.Validate(new BroadcastNotificationRequest(BroadcastAudience.Students, "s", "b")).IsValid);
        Assert.False(validator.Validate(new BroadcastNotificationRequest((BroadcastAudience)42, "s", "b")).IsValid);
        Assert.False(validator.Validate(new BroadcastNotificationRequest(BroadcastAudience.Everyone, "", "b")).IsValid);
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(60, true)]
    [InlineData(61, false)]
    public void Invoice_reminder_window_is_0_to_60_days(int days, bool valid)
    {
        Assert.Equal(valid, new SendInvoiceRemindersValidator().Validate(new SendInvoiceRemindersRequest(days)).IsValid);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(168, true)]
    [InlineData(169, false)]
    public void Lesson_reminder_window_is_1_to_168_hours(int hours, bool valid)
    {
        Assert.Equal(valid, new SendLessonRemindersValidator().Validate(new SendLessonRemindersRequest(hours)).IsValid);
    }
}
