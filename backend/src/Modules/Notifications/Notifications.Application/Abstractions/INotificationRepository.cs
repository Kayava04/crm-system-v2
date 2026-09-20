using Notifications.Domain.Entities;
using Shared.Kernel.Abstractions;

namespace Notifications.Application.Abstractions;

public interface INotificationRepository : IRepository<Notification>
{
    Task<(IReadOnlyList<Notification> Notifications, int TotalCount)> GetByRecipientAsync(
        Guid recipientUserId,
        bool unreadOnly,
        int page,
        int pageSize,
        CancellationToken ct = default
    );

    Task<int> CountUnreadAsync(Guid recipientUserId, CancellationToken ct = default);

    Task<IReadOnlyList<Notification>> GetUnreadAsync(
        Guid recipientUserId,
        Contracts.NotificationType? type = null,
        CancellationToken ct = default
    );
}
