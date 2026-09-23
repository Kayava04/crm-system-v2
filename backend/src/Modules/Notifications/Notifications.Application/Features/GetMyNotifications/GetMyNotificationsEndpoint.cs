using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Notifications.Application.Abstractions;
using Notifications.Contracts;
using Shared.Kernel.Common;

namespace Notifications.Application.Features.GetMyNotifications;

public sealed record NotificationResponse(
    Guid Id,
    NotificationType Type,
    string Subject,
    string Body,
    NotificationAction Action,
    bool IsRead,
    DateTime CreatedAt,
    DateTime? ReadAt
);

public static class GetMyNotificationsEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/", Handle)
             .RequireAuthorization()
             .WithName("GetMyNotifications")
             .WithSummary("Get notifications of the current user")
             .Produces<PagedResponse<NotificationResponse>>(StatusCodes.Status200OK)
             .ProducesProblem(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> Handle(
        ClaimsPrincipal user,
        INotificationRepository repository,
        CancellationToken ct,
        bool unreadOnly = false,
        int page = 1,
        int pageSize = 20
    )
    {
        var userId = GetUserId(user);
        if (userId is null)
            return Results.Problem(
                detail: "Invalid user identity.",
                statusCode: StatusCodes.Status401Unauthorized
            );

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 100) pageSize = 100;

        var (notifications, totalCount) = await repository.GetByRecipientAsync(
            userId.Value, unreadOnly, page, pageSize, ct);

        var items = notifications.Select(n => new NotificationResponse(
            n.Id,
            n.Type,
            n.Subject,
            n.Body,
            n.Action,
            n.IsRead,
            n.CreatedAt,
            n.ReadAt)
        ).ToList();

        var response = new PagedResponse<NotificationResponse>(
            items,
            page,
            pageSize,
            totalCount,
            (int)Math.Ceiling(totalCount / (double)pageSize)
        );

        return Results.Ok(response);
    }

    // JwtBearer maps the "sub" claim to NameIdentifier by default
    private static Guid? GetUserId(ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub"), out var id)
            ? id
            : null;
}
