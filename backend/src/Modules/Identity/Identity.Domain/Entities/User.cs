using Microsoft.AspNetCore.Identity;

namespace Identity.Domain.Entities;

public sealed class User : IdentityUser<Guid>
{
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool MustChangePassword { get; private set; }

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

    public void CompletePasswordChange()
    {
        MustChangePassword = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
