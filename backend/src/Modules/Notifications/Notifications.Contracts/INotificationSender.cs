namespace Notifications.Contracts;

public interface INotificationSender
{
    Task SendAsync(
        Guid recipientUserId,
        NotificationType type,
        string subject,
        string body,
        NotificationAction action = NotificationAction.None,
        CancellationToken ct = default
    );

    Task ResolveAsync(
        Guid recipientUserId,
        NotificationType type,
        CancellationToken ct = default
    );
}

public enum NotificationType
{
    PasswordChangeRequired,
    InvoiceReminder,
    LessonReminder,
    General
}

public enum NotificationAction
{
    None,
    ChangePassword
}
