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

    // Marks all unread notifications of the given type as read (e.g. once the requested action is done)
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

// Tells the client which action to offer next to the notification
public enum NotificationAction
{
    None,
    ChangePassword
}
