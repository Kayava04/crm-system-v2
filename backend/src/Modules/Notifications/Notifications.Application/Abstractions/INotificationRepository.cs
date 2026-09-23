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

    Task AddRangeAsync(IReadOnlyCollection<Notification> notifications, CancellationToken ct = default);

    // Which of the given (user, key) pairs have already been sent for this type
    Task<HashSet<(Guid UserId, string Key)>> GetSentKeysAsync(
        Contracts.NotificationType type,
        IReadOnlyCollection<string> keys,
        CancellationToken ct = default
    );

    Task<(IReadOnlyList<Notification> Notifications, int TotalCount)> GetAllAsync(
        Guid? recipientUserId,
        Contracts.NotificationType? type,
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
