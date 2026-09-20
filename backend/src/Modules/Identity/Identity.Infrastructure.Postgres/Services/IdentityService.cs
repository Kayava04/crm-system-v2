using Identity.Application.Abstractions;
using Identity.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Identity.Infrastructure.Postgres.Services;

internal sealed class IdentityService(UserManager<User> userManager) : IIdentityService
{
    public async Task<User> CreateUserAsync(
        string email,
        string password,
        bool mustChangePassword = true,
        CancellationToken ct = default
    )
    {
        var user = User.Create(email, mustChangePassword);
        var result = await userManager.CreateAsync(user, password);

        if (result.Errors.Any(e => e.Code is "DuplicateEmail" or "DuplicateUserName"))
            throw new UserAlreadyExistsException(string.Join("; ", result.Errors.Select(e => e.Description)));

        if (!result.Succeeded)
            throw new InvalidOperationException(
                string.Join("; ", result.Errors.Select(e => e.Description)));

        return user;
    }

    public async Task<TimeSpan?> GetLockoutRemainingAsync(User user, CancellationToken ct = default)
    {
        if (!await userManager.IsLockedOutAsync(user))
            return null;

        var end = await userManager.GetLockoutEndDateAsync(user);

        return end is null ? null : end.Value - DateTimeOffset.UtcNow;
    }

    public async Task RecordFailedLoginAsync(User user, CancellationToken ct = default) =>
        await userManager.AccessFailedAsync(user);

    public async Task RecordSuccessfulLoginAsync(User user, CancellationToken ct = default)
    {
        if (user.AccessFailedCount > 0)
            await userManager.ResetAccessFailedCountAsync(user);
    }

    public async Task UnlockAsync(User user, CancellationToken ct = default)
    {
        await userManager.SetLockoutEndDateAsync(user, null);
        await userManager.ResetAccessFailedCountAsync(user);
    }

    public async Task<bool> CheckPasswordAsync(User user, string password, CancellationToken ct = default) =>
        await userManager.CheckPasswordAsync(user, password);

    public async Task ResetPasswordAsync(User user, string newPassword, CancellationToken ct = default)
    {
        var removeResult = await userManager.RemovePasswordAsync(user);

        if (!removeResult.Succeeded)
            throw new InvalidOperationException(
                string.Join("; ", removeResult.Errors.Select(e => e.Description)));

        var addResult = await userManager.AddPasswordAsync(user, newPassword);

        if (!addResult.Succeeded)
            throw new InvalidOperationException(
                string.Join("; ", addResult.Errors.Select(e => e.Description)));
    }

    public async Task ChangePasswordAsync(User user, string oldPassword, string newPassword, CancellationToken ct = default)
    {
        var result = await userManager.ChangePasswordAsync(user, oldPassword, newPassword);

        if (!result.Succeeded)
            throw new InvalidOperationException(
                string.Join("; ", result.Errors.Select(e => e.Description)));
    }
}
