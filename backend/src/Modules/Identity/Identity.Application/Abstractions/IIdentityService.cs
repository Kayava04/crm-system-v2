using Identity.Domain.Entities;

namespace Identity.Application.Abstractions;

public interface IIdentityService
{
    Task<User> CreateUserAsync(
        string email,
        string password,
        bool mustChangePassword = true,
        CancellationToken ct = default
    );

    Task<TimeSpan?> GetLockoutRemainingAsync(User user, CancellationToken ct = default);
    Task RecordFailedLoginAsync(User user, CancellationToken ct = default);
    Task RecordSuccessfulLoginAsync(User user, CancellationToken ct = default);
    Task UnlockAsync(User user, CancellationToken ct = default);

    Task<bool> CheckPasswordAsync(User user, string password, CancellationToken ct = default);
    Task ResetPasswordAsync(User user, string newPassword, CancellationToken ct = default);
    Task ChangePasswordAsync(User user, string oldPassword, string newPassword, CancellationToken ct = default);
}
