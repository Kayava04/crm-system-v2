namespace Notifications.Contracts;

// One pass of the periodic job: marks overdue invoices and sends the invoice and lesson reminders.
// The background service calls it on a timer; it is public so it can also be run and tested directly.
public interface INotificationAutomation
{
    Task<AutomationRunResult> RunOnceAsync(CancellationToken ct = default);
}

// Skipped = another instance was already running the job. A failed step is listed in Errors and does not stop the others.
public sealed record AutomationRunResult(
    bool Skipped,
    int InvoicesMarkedOverdue,
    int InvoiceRemindersCreated,
    int LessonRemindersCreated,
    IReadOnlyList<string> Errors
);

public static class NotificationAutomationLock
{
    public const string Name = "crm:notifications-automation";
}
