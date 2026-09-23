using Identity.Application.Abstractions;
using Identity.Contracts;
using Microsoft.Extensions.Logging;

namespace Identity.Application;

internal sealed class UserAccountManager(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IIdentityUnitOfWork unitOfWork,
    ILogger<UserAccountManager> logger
) : IUserAccountManager
{
    public async Task SetActiveAsync(Guid userId, bool isActive, CancellationToken ct = default)
    {
        var user = await userRepository.GetByIdAsync(userId, ct);

        if (user is null || user.IsActive == isActive)
            return;

        user.SetActive(isActive);
        await userRepository.UpdateAsync(user, ct);

        // Sessions of a deactivated account must not survive
        if (!isActive)
            await refreshTokenRepository.RevokeAllForUserAsync(userId, ct);

        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("User account {UserId} {State}", userId, isActive ? "activated" : "deactivated");
    }
}
