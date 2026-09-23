using Notifications.Application.Abstractions;
using Notifications.Contracts;
using Notifications.Domain.Entities;

namespace Notifications.Application;

internal sealed class NotificationSenderService(
    INotificationRepository repository,
    INotificationsUnitOfWork unitOfWork
) : INotificationSender
{
    public async Task SendAsync(
        Guid recipientUserId,
        NotificationType type,
        string subject,
        string body,
        NotificationAction action = NotificationAction.None,
        CancellationToken ct = default)
    {
        var notification = Notification.Create(recipientUserId, type, subject, body, action);

        await repository.AddAsync(notification, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task ResolveAsync(
        Guid recipientUserId,
        NotificationType type,
        CancellationToken ct = default)
    {
        var unread = await repository.GetUnreadAsync(recipientUserId, type, ct);
        if (unread.Count == 0)
            return;

        foreach (var notification in unread)
            notification.MarkRead();

        await unitOfWork.SaveChangesAsync(ct);
    }
}
