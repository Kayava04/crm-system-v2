using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Notifications.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Notifications.Application.Features.MarkAllNotificationsRead;

public sealed record MarkAllNotificationsReadRequest;

public static class MarkAllNotificationsReadEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPut("/read-all", Handle)
             .RequireAuthorization()
             .WithName("MarkAllNotificationsRead")
             .WithSummary("Mark all notifications of the current user as read")
             .Produces(StatusCodes.Status204NoContent)
             .ProducesProblem(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> Handle(
        ClaimsPrincipal user,
        INotificationRepository repository,
        INotificationsUnitOfWork unitOfWork,
        ILogger<MarkAllNotificationsReadRequest> logger,
        CancellationToken ct
    )
    {
        var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Results.Problem(
                detail: "Invalid user identity.",
                statusCode: StatusCodes.Status401Unauthorized
            );

        var unread = await repository.GetUnreadAsync(userId, ct: ct);

        foreach (var notification in unread)
            notification.MarkRead();

        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Marked {Count} notifications as read for user {UserId}", unread.Count, userId);

        return Results.NoContent();
    }
}
