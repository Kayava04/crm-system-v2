using Notifications.Contracts;
using Shared.Kernel.Primitives;

namespace Notifications.Domain.Entities;

public sealed class Notification : AuditableEntity
{
    public Guid RecipientUserId { get; private set; }
    public NotificationType Type { get; private set; }
    public string Subject { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public NotificationAction Action { get; private set; }
    public DateTime? ReadAt { get; private set; }

    public bool IsRead => ReadAt is not null;

    private Notification() { }

    public static Notification Create(
        Guid recipientUserId,
        NotificationType type,
        string subject,
        string body,
        NotificationAction action = NotificationAction.None
    )
    {
        return new Notification
        {
            Id = Guid.NewGuid(),
            RecipientUserId = recipientUserId,
            Type = type,
            Subject = subject,
            Body = body,
            Action = action,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkRead()
    {
        if (IsRead)
            return;

        ReadAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
