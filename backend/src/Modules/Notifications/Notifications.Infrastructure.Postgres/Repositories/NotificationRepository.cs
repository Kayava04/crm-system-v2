using Microsoft.EntityFrameworkCore;
using Notifications.Application.Abstractions;
using Notifications.Contracts;
using Notifications.Domain.Entities;
using Notifications.Infrastructure.Postgres.Persistence;

namespace Notifications.Infrastructure.Postgres.Repositories;

internal sealed class NotificationRepository(NotificationsDbContext context) : INotificationRepository
{
    public async Task<IReadOnlyList<Notification>> GetAllAsync(CancellationToken ct = default) =>
        await context.Notifications
            .AsNoTracking()
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(ct);

    public async Task<Notification?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id, ct);

    public async Task AddAsync(Notification entity, CancellationToken ct = default) =>
        await context.Notifications.AddAsync(entity, ct);

    public Task UpdateAsync(Notification entity, CancellationToken ct = default)
    {
        context.Notifications.Update(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Notification entity, CancellationToken ct = default)
    {
        context.Notifications.Remove(entity);
        return Task.CompletedTask;
    }

    public async Task<(IReadOnlyList<Notification> Notifications, int TotalCount)> GetByRecipientAsync(
        Guid recipientUserId,
        bool unreadOnly,
        int page,
        int pageSize,
        CancellationToken ct = default
    )
    {
        var query = context.Notifications
            .AsNoTracking()
            .Where(n => n.RecipientUserId == recipientUserId);

        if (unreadOnly)
            query = query.Where(n => n.ReadAt == null);

        var totalCount = await query.CountAsync(ct);

        var notifications = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (notifications, totalCount);
    }

    public async Task<int> CountUnreadAsync(Guid recipientUserId, CancellationToken ct = default) =>
        await context.Notifications
            .AsNoTracking()
            .CountAsync(n => n.RecipientUserId == recipientUserId && n.ReadAt == null, ct);

    public async Task<IReadOnlyList<Notification>> GetUnreadAsync(
        Guid recipientUserId,
        NotificationType? type = null,
        CancellationToken ct = default)
    {
        var query = context.Notifications
            .Where(n => n.RecipientUserId == recipientUserId && n.ReadAt == null);

        if (type.HasValue)
            query = query.Where(n => n.Type == type.Value);

        return await query.ToListAsync(ct);
    }
}
