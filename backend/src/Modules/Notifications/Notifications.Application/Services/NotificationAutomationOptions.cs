namespace Notifications.Application.Services;

public sealed class NotificationAutomationOptions
{
    public const string SectionName = "Notifications:Automation";

    public bool Enabled { get; set; }

    public int IntervalSeconds { get; set; } = 900;
    public int StartupDelaySeconds { get; set; } = 30;

    public int LessonReminderHoursAhead { get; set; } = 24;
    public int InvoiceReminderDaysBefore { get; set; } = 3;
    public bool IncludeOverdue { get; set; } = true;
    public bool MarkOverdueInvoices { get; set; } = true;

    internal int SafeIntervalSeconds => Math.Max(1, IntervalSeconds);
    internal int SafeStartupDelaySeconds => Math.Max(0, StartupDelaySeconds);
    internal int SafeLessonHours => Math.Clamp(LessonReminderHoursAhead, 1, 168);
    internal int SafeInvoiceDays => Math.Clamp(InvoiceReminderDaysBefore, 0, 60);
}
