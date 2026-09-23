using Microsoft.Extensions.Logging;
using Notifications.Contracts;

namespace Identity.Application.Services;

// The account is already created / changed at this point, so a notification failure must not fail the request
internal static class PasswordNotifications
{
    public static async Task SendChangeRequiredAsync(
        INotificationSender sender,
        Guid userId,
        ILogger logger,
        CancellationToken ct)
    {
        try
        {
            await sender.SendAsync(
                userId,
                NotificationType.PasswordChangeRequired,
                "Change your temporary password",
                "You are using a temporary password. Please set your own password to keep your account secure.",
                NotificationAction.ChangePassword,
                ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create password change notification for user {UserId}", userId);
        }
    }

    public static async Task ResolveChangeRequiredAsync(
        INotificationSender sender,
        Guid userId,
        ILogger logger,
        CancellationToken ct)
    {
        try
        {
            await sender.ResolveAsync(userId, NotificationType.PasswordChangeRequired, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to resolve password change notification for user {UserId}", userId);
        }
    }
}
