using Billing.Contracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Notifications.Contracts;
using Npgsql;
using Shared.Infrastructure;

namespace Crm.IntegrationTests.Tests;

// The periodic job: overdue invoices and reminders, all delivered as in-app notifications
public class NotificationAutomationTests(CrmApiFactory factory) : ApiTest(factory)
{
    private static string InDays(int days) => DateTime.UtcNow.AddDays(days).ToString("yyyy-MM-dd");

    private INotificationAutomation Automation => Factory.Services.GetRequiredService<INotificationAutomation>();

    // ------------------------------------------------------------------ helpers
    private async Task<(TestUser Student, Guid Enrollment)> StudentWithAccountAsync()
    {
        var studentId = await Data.StudentAsync();
        var user = await Data.StudentUserAsync(studentId);

        return (user, await Data.EnrollmentAsync(studentId, await Data.CourseAsync(price: 3000)));
    }

    private async Task<Guid> InvoiceAsync(Guid enrollment, string period, string due) =>
        (await Api.PostAsync("/api/billing/invoices", new { enrollmentId = enrollment, period, dueDate = due, notes = (string?)null }, Admin)).Expect(201).Id;

    private async Task LessonInAsync(Guid enrollment, Guid teacher, int hours) =>
        (await Api.PostAsync("/api/schedules", new
        {
            enrollmentId = enrollment, groupId = (Guid?)null, teacherId = teacher,
            scheduledDate = DateTime.UtcNow.AddHours(hours), durationMinutes = 60, notes = (string?)null
        }, Admin)).Expect(201);

    private async Task<int> RemindersAsync(string token, string type) =>
        (await Api.GetAsync("/api/notifications?pageSize=100", token)).Expect(200).Items
            .Count(i => i!["type"]!.GetValue<string>() == type);

    private async Task<string?> InvoiceStatus(Guid id) => (await Api.GetAsync($"/api/billing/invoices/{id}", Admin)).Expect(200)["status"].GetValue<string>();

    private WebApplicationFactory<Program> Host(Dictionary<string, string> settings, Action<IServiceCollection>? services = null) =>
        Factory.WithWebHostBuilder(builder =>
        {
            foreach (var (key, value) in settings)
                builder.UseSetting("Notifications:Automation:" + key, value);

            if (services is not null)
                builder.ConfigureTestServices(services);
        });

    private sealed class FailingOverdueMarker : IInvoiceOverdueMarker
    {
        public Task<int> MarkOverdueAsync(CancellationToken ct = default) => throw new InvalidOperationException("Simulated failure.");
    }

    // ------------------------------------------------------------------ one run
    [Fact]
    public async Task One_run_marks_overdue_invoices_and_sends_invoice_and_lesson_reminders()
    {
        var teacherId = await Data.TeacherAsync();
        var teacher = await Data.TeacherUserAsync(teacherId);
        var (late, lateEnrollment) = await StudentWithAccountAsync();
        var (soon, soonEnrollment) = await StudentWithAccountAsync();
        var (learner, learnerEnrollment) = await StudentWithAccountAsync();
        var overdueInvoice = await InvoiceAsync(lateEnrollment, "2020-01", "2020-01-31");
        await InvoiceAsync(soonEnrollment, "2034-01", InDays(2));
        await LessonInAsync(learnerEnrollment, teacherId, 5);

        var result = await Automation.RunOnceAsync();

        Assert.False(result.Skipped);
        Assert.Empty(result.Errors);
        Assert.True(result.InvoicesMarkedOverdue >= 1);
        Assert.True(result.InvoiceRemindersCreated >= 2);
        Assert.True(result.LessonRemindersCreated >= 2);   // the student and the teacher

        Assert.Equal("Overdue", await InvoiceStatus(overdueInvoice));
        Assert.Equal(1, await RemindersAsync(late.Token, "InvoiceReminder"));
        Assert.Equal(1, await RemindersAsync(soon.Token, "InvoiceReminder"));
        Assert.Equal(1, await RemindersAsync(learner.Token, "LessonReminder"));
        Assert.Equal(1, await RemindersAsync(teacher.Token, "LessonReminder"));
    }

    [Fact]
    public async Task Running_again_creates_nothing_new()
    {
        var teacherId = await Data.TeacherAsync();
        var (student, enrollment) = await StudentWithAccountAsync();
        await InvoiceAsync(enrollment, "2034-02", InDays(1));
        await LessonInAsync(enrollment, teacherId, 6);
        await Automation.RunOnceAsync();

        var second = await Automation.RunOnceAsync();

        Assert.Equal(0, second.InvoiceRemindersCreated);
        Assert.Equal(0, second.LessonRemindersCreated);
        Assert.Equal(0, second.InvoicesMarkedOverdue);
        Assert.Equal(1, await RemindersAsync(student.Token, "InvoiceReminder"));
        Assert.Equal(1, await RemindersAsync(student.Token, "LessonReminder"));
    }

    [Fact]
    public async Task Reminders_are_only_for_what_is_due_soon_and_only_for_active_students()
    {
        var teacherId = await Data.TeacherAsync();
        var (student, enrollment) = await StudentWithAccountAsync();
        var paid = await InvoiceAsync(enrollment, "2034-03", InDays(1));
        (await Api.PutAsync($"/api/billing/invoices/{paid}/paid", null, Admin)).Expect(204);
        await InvoiceAsync(enrollment, "2034-04", InDays(45));           // due far in the future
        await LessonInAsync(enrollment, teacherId, 100);                 // outside the 24 hour window

        var (paused, pausedEnrollment) = await StudentWithAccountAsync();
        await LessonInAsync(pausedEnrollment, teacherId, 8);
        (await Api.PutAsync($"/api/enrollments/{pausedEnrollment}/suspend", null, Admin)).Expect(200);   // its lesson is cancelled

        await Automation.RunOnceAsync();

        Assert.Equal(0, await RemindersAsync(student.Token, "InvoiceReminder"));
        Assert.Equal(0, await RemindersAsync(student.Token, "LessonReminder"));
        Assert.Equal(0, await RemindersAsync(paused.Token, "LessonReminder"));
    }

    // ------------------------------------------------------------------ settings
    [Fact]
    public async Task The_settings_decide_what_the_run_does()
    {
        var teacherId = await Data.TeacherAsync();
        var (student, enrollment) = await StudentWithAccountAsync();
        var overdue = await InvoiceAsync(enrollment, "2020-05", "2020-05-31");
        await InvoiceAsync(enrollment, "2034-05", InDays(2));
        await LessonInAsync(enrollment, teacherId, 5);

        // no overdue marking, no overdue reminders, invoices only on their due day, lessons only within 2 hours
        using var host = Host(new()
        {
            ["MarkOverdueInvoices"] = "false", ["IncludeOverdue"] = "false",
            ["InvoiceReminderDaysBefore"] = "0", ["LessonReminderHoursAhead"] = "2"
        });
        var result = await host.Services.GetRequiredService<INotificationAutomation>().RunOnceAsync();

        Assert.Equal(0, result.InvoicesMarkedOverdue);
        Assert.Equal("Pending", await InvoiceStatus(overdue));
        Assert.Equal(0, await RemindersAsync(student.Token, "InvoiceReminder"));
        Assert.Equal(0, await RemindersAsync(student.Token, "LessonReminder"));
    }

    [Fact]
    public async Task Out_of_range_settings_are_brought_back_into_range()
    {
        var teacherId = await Data.TeacherAsync();
        var (student, enrollment) = await StudentWithAccountAsync();
        await LessonInAsync(enrollment, teacherId, 100);   // inside the largest allowed window (168 h)
        await LessonInAsync(enrollment, teacherId, 300);   // beyond it

        using var host = Host(new() { ["LessonReminderHoursAhead"] = "99999", ["InvoiceReminderDaysBefore"] = "-9", ["IntervalSeconds"] = "-5" });
        var result = await host.Services.GetRequiredService<INotificationAutomation>().RunOnceAsync();

        Assert.Empty(result.Errors);
        Assert.Equal(1, await RemindersAsync(student.Token, "LessonReminder"));   // only the 100 h one
    }

    // ------------------------------------------------------------------ failures
    [Fact]
    public async Task A_failing_step_is_reported_and_the_other_steps_still_run()
    {
        var teacherId = await Data.TeacherAsync();
        var (student, enrollment) = await StudentWithAccountAsync();
        await LessonInAsync(enrollment, teacherId, 5);

        using var host = Host([], services =>
        {
            services.RemoveAll<IInvoiceOverdueMarker>();
            services.AddScoped<IInvoiceOverdueMarker, FailingOverdueMarker>();
        });
        var result = await host.Services.GetRequiredService<INotificationAutomation>().RunOnceAsync();

        Assert.Single(result.Errors);
        Assert.Contains("mark overdue invoices", result.Errors[0]);
        Assert.True(result.LessonRemindersCreated >= 1);
        Assert.Equal(1, await RemindersAsync(student.Token, "LessonReminder"));
    }

    // ------------------------------------------------------------------ several instances
    private async Task<NpgsqlConnection> HoldTheLockAsync()
    {
        var connection = new NpgsqlConnection(Factory.Database.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("select pg_advisory_lock(@key)", connection);
        command.Parameters.AddWithValue("key", AdvisoryLockKey.For(NotificationAutomationLock.Name));
        await command.ExecuteNonQueryAsync();

        return connection;
    }

    [Fact]
    public async Task While_another_instance_holds_the_lock_the_run_is_skipped()
    {
        var teacherId = await Data.TeacherAsync();
        var (student, enrollment) = await StudentWithAccountAsync();
        await LessonInAsync(enrollment, teacherId, 5);

        await using (var other = await HoldTheLockAsync())
        {
            var skipped = await Automation.RunOnceAsync();

            Assert.True(skipped.Skipped);
            Assert.Equal(0, skipped.LessonRemindersCreated);
            Assert.Equal(0, await RemindersAsync(student.Token, "LessonReminder"));
        }   // the other instance is gone: the lock is free again

        var ran = await Automation.RunOnceAsync();

        Assert.False(ran.Skipped);
        Assert.Equal(1, await RemindersAsync(student.Token, "LessonReminder"));
    }

    [Fact]
    public async Task Two_runs_at_the_same_moment_never_send_a_reminder_twice()
    {
        var teacherId = await Data.TeacherAsync();
        var (student, enrollment) = await StudentWithAccountAsync();
        await LessonInAsync(enrollment, teacherId, 5);
        await InvoiceAsync(enrollment, "2034-06", InDays(1));

        var results = await Task.WhenAll(Automation.RunOnceAsync(), Automation.RunOnceAsync());

        Assert.Contains(results, r => !r.Skipped);
        Assert.Equal(1, await RemindersAsync(student.Token, "LessonReminder"));
        Assert.Equal(1, await RemindersAsync(student.Token, "InvoiceReminder"));
    }

    [Fact]
    public async Task The_lock_is_released_after_every_run()
    {
        for (var i = 0; i < 3; i++)
            Assert.False((await Automation.RunOnceAsync()).Skipped);

        Assert.Equal("0", await Sql($"select count(*) from pg_locks where locktype = 'advisory' and objid = {AdvisoryLockKey.For(NotificationAutomationLock.Name) & 0xFFFFFFFF}"));
    }

    // ------------------------------------------------------------------ the timer itself
    private async Task<bool> WaitUntilAsync(Func<Task<bool>> condition, int seconds = 25)
    {
        var until = DateTime.UtcNow.AddSeconds(seconds);

        while (DateTime.UtcNow < until)
        {
            if (await condition())
                return true;

            await Task.Delay(300);
        }

        return false;
    }

    [Fact]
    public async Task When_enabled_the_timer_sends_the_reminders_by_itself()
    {
        var teacherId = await Data.TeacherAsync();
        var (student, enrollment) = await StudentWithAccountAsync();
        var overdue = await InvoiceAsync(enrollment, "2020-07", "2020-07-31");
        await LessonInAsync(enrollment, teacherId, 5);

        using var host = Host(new() { ["Enabled"] = "true", ["IntervalSeconds"] = "1", ["StartupDelaySeconds"] = "0" });
        _ = host.Services;   // starting the host starts the timer

        Assert.True(await WaitUntilAsync(async () => await RemindersAsync(student.Token, "LessonReminder") == 1), "no lesson reminder arrived");
        Assert.True(await WaitUntilAsync(async () => await InvoiceStatus(overdue) == "Overdue"), "the invoice was not marked overdue");
        Assert.True(await WaitUntilAsync(async () => await RemindersAsync(student.Token, "InvoiceReminder") == 1), "no invoice reminder arrived");

        await Task.Delay(2500);   // a few more ticks: still exactly one of each
        Assert.Equal(1, await RemindersAsync(student.Token, "LessonReminder"));
        Assert.Equal(1, await RemindersAsync(student.Token, "InvoiceReminder"));
    }

    [Fact]
    public async Task When_disabled_nothing_happens_by_itself()
    {
        var teacherId = await Data.TeacherAsync();
        var (student, enrollment) = await StudentWithAccountAsync();
        var overdue = await InvoiceAsync(enrollment, "2020-08", "2020-08-31");
        await LessonInAsync(enrollment, teacherId, 5);

        using var host = Host(new() { ["Enabled"] = "false", ["IntervalSeconds"] = "1", ["StartupDelaySeconds"] = "0" });
        _ = host.Services;
        await Task.Delay(3000);

        Assert.Equal("Pending", await InvoiceStatus(overdue));
        Assert.Equal(0, await RemindersAsync(student.Token, "LessonReminder"));
    }

    [Fact]
    public async Task A_run_that_throws_does_not_stop_the_timer()
    {
        var flaky = new FlakyAutomation();

        using var host = Host(new() { ["Enabled"] = "true", ["IntervalSeconds"] = "1", ["StartupDelaySeconds"] = "0" }, services =>
        {
            services.RemoveAll<INotificationAutomation>();
            services.AddSingleton<INotificationAutomation>(flaky);
        });
        _ = host.Services;

        Assert.True(await WaitUntilAsync(() => Task.FromResult(flaky.Calls >= 3), 15), $"the timer stopped after {flaky.Calls} call(s)");
    }

    private sealed class FlakyAutomation : INotificationAutomation
    {
        private int _calls;
        public int Calls => _calls;

        public Task<AutomationRunResult> RunOnceAsync(CancellationToken ct = default)
        {
            // the first run fails (e.g. the database blinked), the following ones work
            if (Interlocked.Increment(ref _calls) == 1)
                throw new InvalidOperationException("Simulated failure of a whole run.");

            return Task.FromResult(new AutomationRunResult(false, 0, 0, 0, []));
        }
    }
}
