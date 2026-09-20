using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Notifications.Application.Abstractions;

namespace Notifications.Application.Features.GetUnreadCount;

public sealed record UnreadCountResponse(int Count);

public static class GetUnreadCountEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/unread-count", Handle)
             .RequireAuthorization()
             .WithName("GetUnreadNotificationsCount")
             .WithSummary("Get the number of unread notifications of the current user")
             .Produces<UnreadCountResponse>(StatusCodes.Status200OK)
             .ProducesProblem(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> Handle(
        ClaimsPrincipal user,
        INotificationRepository repository,
        CancellationToken ct
    )
    {
        var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Results.Problem(
                detail: "Invalid user identity.",
                statusCode: StatusCodes.Status401Unauthorized
            );

        var count = await repository.CountUnreadAsync(userId, ct);

        return Results.Ok(new UnreadCountResponse(count));
    }
}
