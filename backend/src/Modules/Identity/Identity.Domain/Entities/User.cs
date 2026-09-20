using Microsoft.AspNetCore.Identity;

namespace Identity.Domain.Entities;

public sealed class User : IdentityUser<Guid>
{
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool MustChangePassword { get; private set; }

    // A deactivated account keeps all its data but cannot log in
    public bool IsActive { get; private set; } = true;

    private User() { }

    public static User Create(string email, bool mustChangePassword = true)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email,
            CreatedAt = DateTime.UtcNow,
            MustChangePassword = mustChangePassword
        };
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RequirePasswordChange()
    {
        MustChangePassword = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void CompletePasswordChange()
    {
        MustChangePassword = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
