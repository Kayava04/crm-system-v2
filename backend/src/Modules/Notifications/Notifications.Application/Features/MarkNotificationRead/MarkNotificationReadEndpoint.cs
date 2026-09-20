using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Notifications.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Notifications.Application.Features.MarkNotificationRead;

public sealed record MarkNotificationReadRequest;

public static class MarkNotificationReadEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPut("/{id:guid}/read", Handle)
             .RequireAuthorization()
             .WithName("MarkNotificationRead")
             .WithSummary("Mark a notification as read")
             .Produces(StatusCodes.Status204NoContent)
             .ProducesProblem(StatusCodes.Status401Unauthorized)
             .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        Guid id,
        ClaimsPrincipal user,
        INotificationRepository repository,
        INotificationsUnitOfWork unitOfWork,
        ILogger<MarkNotificationReadRequest> logger,
        CancellationToken ct
    )
    {
        var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Results.Problem(
                detail: "Invalid user identity.",
                statusCode: StatusCodes.Status401Unauthorized
            );

        var notification = await repository.GetByIdAsync(id, ct);

        // Someone else's notification is reported as not found so its existence is not leaked
        if (notification is null || notification.RecipientUserId != userId)
            return Results.Problem(
                detail: $"Notification with id '{id}' not found.",
                statusCode: StatusCodes.Status404NotFound
            );

        notification.MarkRead();

        await repository.UpdateAsync(notification, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Notification {NotificationId} marked as read", id);

        return Results.NoContent();
    }
}
