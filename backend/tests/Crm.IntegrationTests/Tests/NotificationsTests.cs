namespace Crm.IntegrationTests.Tests;

public class NotificationsTests(CrmApiFactory factory) : ApiTest(factory)
{
    private static string InDays(int days) => DateTime.UtcNow.AddDays(days).ToString("yyyy-MM-dd");

    private async Task<string> NotificationsAdminAsync() =>
        (await Data.UserAsync("Admin", permissionIds: await Data.PermissionIdsAsync("CanManageNotifications"))).Token;

    private async Task<JsonArray> InboxAsync(string token, string? type = null) =>
        (await Api.GetAsync("/api/notifications?pageSize=100" + (type is null ? "" : "&unreadOnly=false"), token)).Expect(200).Items;

    private static int OfType(JsonArray items, string type) => items.Count(i => i!["type"]!.GetValue<string>() == type);

    // ------------------------------------------------------------------ the user's own inbox
    [Fact]
    public async Task Users_read_mark_and_clear_their_own_notifications()
    {
        var user = await Data.UserAsync("Student");
        var admin = await NotificationsAdminAsync();
        (await Api.PostAsync("/api/notifications/send", new { recipientUserIds = new[] { user.UserId }, subject = "Second", body = "b" }, admin)).Expect(200);

        var unread = (await Api.GetAsync("/api/notifications?unreadOnly=true", user.Token)).Expect(200);
        Assert.Equal(2, unread["totalCount"].GetValue<int>());   // the password one and the message
        var first = unread.Items[0]!["id"]!.GetValue<Guid>();

        (await Api.PutAsync($"/api/notifications/{first}/read", null, user.Token)).Expect(204);
        Assert.Equal(1, (await Api.GetAsync("/api/notifications/unread-count", user.Token))["count"].GetValue<int>());
        Assert.Equal(2, (await Api.GetAsync("/api/notifications?unreadOnly=false", user.Token))["totalCount"].GetValue<int>());

        (await Api.PutAsync("/api/notifications/read-all", null, user.Token)).Expect(204);
        Assert.Equal(0, (await Api.GetAsync("/api/notifications/unread-count", user.Token))["count"].GetValue<int>());
    }

    [Fact]
    public async Task Nobody_can_read_or_mark_another_users_notification()
    {
        var owner = await Data.UserAsync("Student");
        var stranger = await Data.UserAsync("Student");
        var id = (await InboxAsync(owner.Token))[0]!["id"]!.GetValue<Guid>();

        (await Api.PutAsync($"/api/notifications/{id}/read", null, stranger.Token)).Expect(404);
        (await Api.PutAsync($"/api/notifications/{Guid.NewGuid()}/read", null, owner.Token)).Expect(404);
        Assert.DoesNotContain(await InboxAsync(stranger.Token), i => i!["id"]!.GetValue<Guid>() == id);
        (await Api.GetAsync("/api/notifications")).Expect(401);
    }

    // ------------------------------------------------------------------ who may manage notifications
    [Fact]
    public async Task Only_an_admin_with_the_notifications_permission_can_manage_notifications()
    {
        var student = await Data.UserAsync("Student");
        var teacher = await Data.TeacherUserAsync(await Data.TeacherAsync());
        var plainAdmin = await Data.UserAsync("Admin");   // an admin without the permission
        var body = new { recipientUserIds = new[] { student.UserId }, subject = "s", body = "b" };

        foreach (var (path, payload) in new (string, object?)[]
                 {
                     ("/api/notifications/send", body),
                     ("/api/notifications/broadcast", new { audience = "Students", subject = "s", body = "b" }),
                     ("/api/notifications/invoice-reminders", new { }),
                     ("/api/notifications/lesson-reminders", new { }),
                 })
        {
            (await Api.PostAsync(path, payload)).Expect(401);
            (await Api.PostAsync(path, payload, student.Token)).Expect(403);
            (await Api.PostAsync(path, payload, teacher.Token)).Expect(403);
            (await Api.PostAsync(path, payload, plainAdmin.Token)).Expect(403);
            (await Api.PostAsync(path, payload, Admin)).Expect(200);   // SuperAdmin has every permission
        }

        (await Api.GetAsync("/api/notifications/all", student.Token)).Expect(403);
        (await Api.GetAsync("/api/notifications/all", plainAdmin.Token)).Expect(403);
        (await Api.GetAsync("/api/notifications/all", await NotificationsAdminAsync())).Expect(200);
    }

    // ------------------------------------------------------------------ messages
    [Fact]
    public async Task An_admin_can_message_specific_users_and_review_what_was_sent()
    {
        var admin = await NotificationsAdminAsync();
        var one = await Data.UserAsync("Student");
        var two = await Data.UserAsync("Teacher");

        var sent = (await Api.PostAsync("/api/notifications/send", new { recipientUserIds = new[] { one.UserId, two.UserId, one.UserId }, subject = "Holiday", body = "School is closed on Monday." }, admin)).Expect(200);

        Assert.Equal(2, sent["created"].GetValue<int>());   // duplicates in the request are ignored
        var received = (await InboxAsync(one.Token)).Single(i => i!["subject"]!.GetValue<string>() == "Holiday")!;
        Assert.Equal("General", received["type"]!.GetValue<string>());
        Assert.Equal("School is closed on Monday.", received["body"]!.GetValue<string>());

        var review = (await Api.GetAsync($"/api/notifications/all?recipientUserId={one.UserId}&type=General", admin)).Expect(200);
        Assert.Equal(1, review["totalCount"].GetValue<int>());
        Assert.Equal(one.UserId, review.Items[0]!["recipientUserId"]!.GetValue<Guid>());
    }

    [Fact]
    public async Task Sending_validates_recipients_subject_and_body()
    {
        var admin = await NotificationsAdminAsync();
        var user = await Data.UserAsync("Student");

        (await Api.PostAsync("/api/notifications/send", new { recipientUserIds = Array.Empty<Guid>(), subject = "s", body = "b" }, admin)).Expect(400);
        (await Api.PostAsync("/api/notifications/send", new { recipientUserIds = new[] { user.UserId }, subject = "", body = "b" }, admin)).Expect(400);
        (await Api.PostAsync("/api/notifications/send", new { recipientUserIds = new[] { user.UserId }, subject = "s", body = new string('x', 2001) }, admin)).Expect(400);
        (await Api.PostAsync("/api/notifications/broadcast", new { audience = "Nobody", subject = "s", body = "b" }, admin)).Expect(400);
    }

    [Fact]
    public async Task A_broadcast_reaches_every_active_user_of_the_audience_and_nobody_else()
    {
        var admin = await NotificationsAdminAsync();
        var activeStudent = await Data.UserAsync("Student");
        var withdrawnId = await Data.StudentAsync();
        var withdrawn = await Data.StudentUserAsync(withdrawnId);
        (await Api.PutAsync($"/api/students/{withdrawnId}/status", new { status = "Withdrawn" }, Admin)).Expect(200);
        var teacher = await Data.UserAsync("Teacher");
        var subject = "Broadcast " + TestData.Unique();

        var expected = await Sql(@"select count(*) from identity.users u
            join identity.user_roles ur on ur.""UserId"" = u.""Id"" join identity.roles r on r.""Id"" = ur.""RoleId""
            where r.""Name"" = 'Student' and u.""IsActive""");
        var sent = (await Api.PostAsync("/api/notifications/broadcast", new { audience = "Students", subject, body = "Hello students" }, admin)).Expect(200);

        Assert.Equal(int.Parse(expected!), sent["created"].GetValue<int>());
        Assert.Contains(await InboxAsync(activeStudent.Token), i => i!["subject"]!.GetValue<string>() == subject);
        Assert.DoesNotContain(await InboxAsync(teacher.Token), i => i!["subject"]!.GetValue<string>() == subject);
        Assert.Equal(0, (await Api.GetAsync($"/api/notifications/all?recipientUserId={withdrawn.UserId}&type=General", admin))["totalCount"].GetValue<int>());
    }

    // ------------------------------------------------------------------ invoice reminders
    private async Task<(TestUser Student, Guid Enrollment)> StudentWithAccountAsync()
    {
        var studentId = await Data.StudentAsync();
        var user = await Data.StudentUserAsync(studentId);

        return (user, await Data.EnrollmentAsync(studentId, await Data.CourseAsync(price: 3000)));
    }

    private Task<ApiResponse> Invoice(Guid enrollment, string period, string due) =>
        Api.PostAsync("/api/billing/invoices", new { enrollmentId = enrollment, period, dueDate = due, notes = (string?)null }, Admin);

    [Fact]
    public async Task Invoice_reminders_go_to_students_with_unpaid_invoices_due_soon_once_only()
    {
        var admin = await NotificationsAdminAsync();
        var (student, enrollment) = await StudentWithAccountAsync();
        (await Invoice(enrollment, "2033-01", InDays(2))).Expect(201);

        var first = (await Api.PostAsync("/api/notifications/invoice-reminders", new { daysBeforeDue = 3 }, admin)).Expect(200);
        var second = (await Api.PostAsync("/api/notifications/invoice-reminders", new { daysBeforeDue = 3 }, admin)).Expect(200);

        Assert.True(first["created"].GetValue<int>() >= 1);
        Assert.Equal(0, second["created"].GetValue<int>());   // the same reminder is never sent twice
        Assert.True(second["skippedAlreadySent"].GetValue<int>() >= 1);

        var inbox = await InboxAsync(student.Token);
        Assert.Equal(1, OfType(inbox, "InvoiceReminder"));
        var reminder = inbox.Single(i => i!["type"]!.GetValue<string>() == "InvoiceReminder")!;
        Assert.Contains("2033-01", reminder["subject"]!.GetValue<string>());
        Assert.Contains("3000", reminder["body"]!.GetValue<string>());
    }

    [Fact]
    public async Task Invoice_reminders_skip_paid_invoices_invoices_due_later_and_students_without_an_account()
    {
        var admin = await NotificationsAdminAsync();
        var (student, enrollment) = await StudentWithAccountAsync();
        var paid = (await Invoice(enrollment, "2033-01", InDays(1))).Expect(201);
        (await Api.PutAsync($"/api/billing/invoices/{paid.Id}/paid", null, Admin)).Expect(204);
        (await Invoice(enrollment, "2033-02", InDays(40))).Expect(201);                                  // due far in the future
        var noAccount = await Data.EnrollmentAsync(await Data.StudentAsync(), await Data.CourseAsync());
        (await Invoice(noAccount, "2033-01", InDays(1))).Expect(201);                                     // the student cannot be reached

        var result = (await Api.PostAsync("/api/notifications/invoice-reminders", new { daysBeforeDue = 3 }, admin)).Expect(200);

        Assert.True(result["skippedNoAccount"].GetValue<int>() >= 1);
        Assert.Equal(0, OfType(await InboxAsync(student.Token), "InvoiceReminder"));
    }

    [Fact]
    public async Task Overdue_invoices_get_their_own_reminder_unless_they_are_excluded()
    {
        var admin = await NotificationsAdminAsync();
        var (student, enrollment) = await StudentWithAccountAsync();
        (await Invoice(enrollment, "2020-01", "2020-01-31")).Expect(201);

        await Api.PostAsync("/api/notifications/invoice-reminders", new { daysBeforeDue = 3, includeOverdue = false }, admin);
        Assert.Equal(0, OfType(await InboxAsync(student.Token), "InvoiceReminder"));

        await Api.PostAsync("/api/notifications/invoice-reminders", new { daysBeforeDue = 3, includeOverdue = true }, admin);
        var reminder = (await InboxAsync(student.Token)).Single(i => i!["type"]!.GetValue<string>() == "InvoiceReminder")!;
        Assert.Contains("overdue", reminder["subject"]!.GetValue<string>());
    }

    [Fact]
    public async Task Invoice_reminder_window_is_validated()
    {
        var admin = await NotificationsAdminAsync();

        (await Api.PostAsync("/api/notifications/invoice-reminders", new { daysBeforeDue = -1 }, admin)).Expect(400);
        (await Api.PostAsync("/api/notifications/invoice-reminders", new { daysBeforeDue = 61 }, admin)).Expect(400);
    }

    // ------------------------------------------------------------------ lesson reminders
    private async Task<Guid> LessonInAsync(Guid enrollment, Guid teacher, int hours) =>
        (await Api.PostAsync("/api/schedules", new
        {
            enrollmentId = enrollment, groupId = (Guid?)null, teacherId = teacher,
            scheduledDate = DateTime.UtcNow.AddHours(hours), durationMinutes = 60, notes = (string?)null
        }, Admin)).Expect(201).Id;

    [Fact]
    public async Task Lesson_reminders_reach_the_student_and_the_teacher_once()
    {
        var admin = await NotificationsAdminAsync();
        var teacherId = await Data.TeacherAsync();
        var teacher = await Data.TeacherUserAsync(teacherId);
        var (student, enrollment) = await StudentWithAccountAsync();
        await LessonInAsync(enrollment, teacherId, 5);
        await LessonInAsync(enrollment, teacherId, 100);   // outside the 24 hour window

        var first = (await Api.PostAsync("/api/notifications/lesson-reminders", new { hoursAhead = 24 }, admin)).Expect(200);
        var second = (await Api.PostAsync("/api/notifications/lesson-reminders", new { hoursAhead = 24 }, admin)).Expect(200);

        Assert.True(first["created"].GetValue<int>() >= 2);
        Assert.Equal(0, second["created"].GetValue<int>());
        Assert.Equal(1, OfType(await InboxAsync(student.Token), "LessonReminder"));
        Assert.Equal(1, OfType(await InboxAsync(teacher.Token), "LessonReminder"));
        var text = (await InboxAsync(student.Token)).Single(i => i!["type"]!.GetValue<string>() == "LessonReminder")!["body"]!.GetValue<string>();
        Assert.Contains("Europe/Kyiv", text);   // shown in school time
    }

    [Fact]
    public async Task A_rescheduled_lesson_is_reminded_again_and_a_paused_student_is_not_reminded()
    {
        var admin = await NotificationsAdminAsync();
        var teacherId = await Data.TeacherAsync();
        var (student, enrollment) = await StudentWithAccountAsync();
        var lesson = await LessonInAsync(enrollment, teacherId, 4);
        await Api.PostAsync("/api/notifications/lesson-reminders", new { hoursAhead = 24 }, admin);

        (await Api.PutAsync($"/api/schedules/{lesson}/reschedule", new { scheduledDate = DateTime.UtcNow.AddHours(6) }, Admin)).Expect(204);
        await Api.PostAsync("/api/notifications/lesson-reminders", new { hoursAhead = 24 }, admin);
        Assert.Equal(2, OfType(await InboxAsync(student.Token), "LessonReminder"));   // the new time is a new reminder

        var (pausedStudent, pausedEnrollment) = await StudentWithAccountAsync();
        await LessonInAsync(pausedEnrollment, teacherId, 8);
        (await Api.PutAsync($"/api/enrollments/{pausedEnrollment}/suspend", null, Admin)).Expect(200);   // cancels the lesson
        await Api.PostAsync("/api/notifications/lesson-reminders", new { hoursAhead = 24 }, admin);
        Assert.Equal(0, OfType(await InboxAsync(pausedStudent.Token), "LessonReminder"));
    }

    [Fact]
    public async Task Members_of_a_group_are_reminded_about_group_lessons()
    {
        var admin = await NotificationsAdminAsync();
        var teacherId = await Data.TeacherAsync();
        var course = await Data.CourseAsync("Group", lessons: 4);
        var s1 = await Data.StudentAsync();
        var member = await Data.StudentUserAsync(s1);
        var group = await Data.GroupAsync(course, teacherId, await Data.EnrollmentAsync(s1, course));
        (await Api.PostAsync("/api/schedules", new { enrollmentId = (Guid?)null, groupId = group, teacherId, scheduledDate = DateTime.UtcNow.AddHours(3), durationMinutes = 60, notes = (string?)null }, Admin)).Expect(201);

        await Api.PostAsync("/api/notifications/lesson-reminders", new { hoursAhead = 24 }, admin);

        var reminder = (await InboxAsync(member.Token)).Single(i => i!["type"]!.GetValue<string>() == "LessonReminder")!;
        Assert.Contains("group lesson", reminder["body"]!.GetValue<string>());
    }

    [Fact]
    public async Task Lesson_reminder_window_is_validated_and_an_empty_window_is_fine()
    {
        var admin = await NotificationsAdminAsync();

        (await Api.PostAsync("/api/notifications/lesson-reminders", new { hoursAhead = 0 }, admin)).Expect(400);
        (await Api.PostAsync("/api/notifications/lesson-reminders", new { hoursAhead = 169 }, admin)).Expect(400);
        (await Api.PostAsync("/api/notifications/lesson-reminders", new { hoursAhead = 1 }, admin)).Expect(200);
    }
}
