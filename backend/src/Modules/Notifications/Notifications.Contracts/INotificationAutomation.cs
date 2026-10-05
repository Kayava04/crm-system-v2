namespace Notifications.Contracts;

public interface INotificationAutomation
{
    Task<AutomationRunResult> RunOnceAsync(CancellationToken ct = default);
}

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
