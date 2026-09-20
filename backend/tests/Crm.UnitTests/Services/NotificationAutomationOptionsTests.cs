using Notifications.Application.Services;
using Shared.Infrastructure;

namespace Crm.UnitTests.Services;

public class NotificationAutomationOptionsTests
{
    [Fact]
    public void Automation_is_off_by_default_with_the_agreed_windows()
    {
        var options = new NotificationAutomationOptions();

        Assert.False(options.Enabled);
        Assert.Equal(900, options.IntervalSeconds);
        Assert.Equal(24, options.LessonReminderHoursAhead);
        Assert.Equal(3, options.InvoiceReminderDaysBefore);
        Assert.True(options.IncludeOverdue);
        Assert.True(options.MarkOverdueInvoices);
    }

    [Theory]
    [InlineData(-5, 1)]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(900, 900)]
    public void Interval_is_at_least_one_second(int configured, int expected)
    {
        Assert.Equal(expected, new NotificationAutomationOptions { IntervalSeconds = configured }.SafeIntervalSeconds);
    }

    [Theory]
    [InlineData(-3, 1)]
    [InlineData(0, 1)]
    [InlineData(24, 24)]
    [InlineData(168, 168)]
    [InlineData(99999, 168)]
    public void Lesson_window_stays_within_what_the_admin_endpoint_accepts(int configured, int expected)
    {
        Assert.Equal(expected, new NotificationAutomationOptions { LessonReminderHoursAhead = configured }.SafeLessonHours);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, 0)]
    [InlineData(3, 3)]
    [InlineData(60, 60)]
    [InlineData(500, 60)]
    public void Invoice_window_stays_within_what_the_admin_endpoint_accepts(int configured, int expected)
    {
        Assert.Equal(expected, new NotificationAutomationOptions { InvoiceReminderDaysBefore = configured }.SafeInvoiceDays);
    }

    [Fact]
    public void Startup_delay_is_never_negative()
    {
        Assert.Equal(0, new NotificationAutomationOptions { StartupDelaySeconds = -10 }.SafeStartupDelaySeconds);
    }
}

public class AdvisoryLockKeyTests
{
    [Fact]
    public void The_key_of_a_name_never_changes()
    {
        // every instance must compute the same key, otherwise the lock would not be shared
        Assert.Equal(AdvisoryLockKey.For("crm:notifications-automation"), AdvisoryLockKey.For("crm:notifications-automation"));
        Assert.Equal(unchecked((long)0xAF63DC4C8601EC8C), AdvisoryLockKey.For("a"));   // the published FNV-1a 64-bit value of "a"
    }

    [Fact]
    public void Different_names_have_different_keys()
    {
        Assert.NotEqual(AdvisoryLockKey.For("job-a"), AdvisoryLockKey.For("job-b"));
    }
}
