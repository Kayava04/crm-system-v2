using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Notifications.Application.Abstractions;
using Notifications.Contracts;
using Shared.Kernel.Common;

namespace Notifications.Application.Features.GetAllNotifications;

public sealed record AdminNotificationResponse(
    Guid Id,
    Guid RecipientUserId,
    NotificationType Type,
    string Subject,
    string Body,
    bool IsRead,
    DateTime CreatedAt,
    DateTime? ReadAt
);

public static class GetAllNotificationsEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/all", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanManageNotifications))
             .WithName("GetAllNotifications")
             .WithSummary("Get notifications of all users (for administrators)")
             .Produces<PagedResponse<AdminNotificationResponse>>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> Handle(
        INotificationRepository repository,
        CancellationToken ct,
        Guid? recipientUserId = null,
        NotificationType? type = null,
        bool unreadOnly = false,
        int page = 1,
        int pageSize = 20
    )
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 100) pageSize = 100;

        var (notifications, totalCount) = await repository.GetAllAsync(
            recipientUserId, type, unreadOnly, page, pageSize, ct);

        var items = notifications.Select(n => new AdminNotificationResponse(
            n.Id, n.RecipientUserId, n.Type, n.Subject, n.Body, n.IsRead, n.CreatedAt, n.ReadAt)
        ).ToList();

        var response = new PagedResponse<AdminNotificationResponse>(
            items,
            page,
            pageSize,
            totalCount,
            (int)Math.Ceiling(totalCount / (double)pageSize)
        );

        return Results.Ok(response);
    }
}
