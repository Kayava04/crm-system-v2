using Identity.Application.Abstractions;
using Identity.Application.Services;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Notifications.Contracts;

namespace Identity.Application.Features.ResetPassword;

public sealed record ResetPasswordRequest;

public sealed record ResetPasswordResponse(
    Guid UserId,
    string Email,
    string TemporaryPassword
);

public static class ResetPasswordEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/users/{id:guid}/reset-password", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanManageAdmins))
             .WithName("ResetPassword")
             .WithSummary("Reset a user's password to a new temporary one")
             .Produces<ResetPasswordResponse>(StatusCodes.Status200OK)
             .ProducesProblem(StatusCodes.Status401Unauthorized)
             .ProducesProblem(StatusCodes.Status403Forbidden)
             .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        Guid id,
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IIdentityService identityService,
        IIdentityUnitOfWork unitOfWork,
        INotificationSender notificationSender,
        ILogger<ResetPasswordRequest> logger,
        CancellationToken ct
    )
    {
        var user = await userRepository.GetByIdAsync(id, ct);
        if (user is null)
            return Results.Problem(
                detail: $"User with id '{id}' not found.",
                statusCode: StatusCodes.Status404NotFound
            );

        // A CanManageAdmins holder must not be able to take over the bootstrap account
        var roles = await userRepository.GetUserRolesAsync(id, ct);
        if (roles.Any(r => r.Name == nameof(SystemRole.SuperAdmin)))
        {
            logger.LogWarning("Attempt to reset password of SuperAdmin {UserId}", id);

            return Results.Problem(
                detail: "Password of a SuperAdmin cannot be reset.",
                statusCode: StatusCodes.Status403Forbidden
            );
        }

        var temporaryPassword = TemporaryPasswordGenerator.Generate();

        await identityService.ResetPasswordAsync(user, temporaryPassword, ct);

        user.RequirePasswordChange();

        await userRepository.UpdateAsync(user, ct);
        // Existing sessions must not survive a reset
        await refreshTokenRepository.RevokeAllForUserAsync(id, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Password reset for user {UserId}", id);

        await PasswordNotifications.SendChangeRequiredAsync(notificationSender, id, logger, ct);

        return Results.Ok(new ResetPasswordResponse(user.Id, user.Email!, temporaryPassword));
    }
}
