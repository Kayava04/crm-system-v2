using Notifications.Application.Abstractions;
using Notifications.Contracts;
using Notifications.Domain.Entities;

namespace Notifications.Application.Services;

internal sealed record NotificationDraft(Guid RecipientUserId, string Subject, string Body, string? ReferenceKey = null);

internal sealed record DispatchResult(int Created, int Duplicates);

// Saves a batch of notifications; drafts with a reference key are skipped when that user already got it
internal sealed class NotificationDispatcher(
    INotificationRepository repository,
    INotificationsUnitOfWork unitOfWork)
{
    public async Task<DispatchResult> DispatchAsync(
        NotificationType type,
        IReadOnlyCollection<NotificationDraft> drafts,
        NotificationAction action = NotificationAction.None,
        CancellationToken ct = default)
    {
        var keys = drafts.Where(d => d.ReferenceKey is not null).Select(d => d.ReferenceKey!).Distinct().ToList();
        var alreadySent = keys.Count == 0
            ? []
            : await repository.GetSentKeysAsync(type, keys, ct);

        var seen = new HashSet<(Guid, string)>();
        var toCreate = new List<Notification>();
        var duplicates = 0;

        foreach (var draft in drafts)
        {
            if (draft.ReferenceKey is not null
                && (alreadySent.Contains((draft.RecipientUserId, draft.ReferenceKey))
                    || !seen.Add((draft.RecipientUserId, draft.ReferenceKey))))
            {
                duplicates++;
                continue;
            }

            toCreate.Add(Notification.Create(
                draft.RecipientUserId, type, draft.Subject, draft.Body, action, draft.ReferenceKey));
        }

        if (toCreate.Count > 0)
        {
            await repository.AddRangeAsync(toCreate, ct);
            await unitOfWork.SaveChangesAsync(ct);
        }

        return new DispatchResult(toCreate.Count, duplicates);
    }
}
