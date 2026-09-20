using Microsoft.AspNetCore.Identity;

namespace Identity.Domain.Entities;

public sealed class User : IdentityUser<Guid>
{
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool MustChangePassword { get; private set; }

    // A deactivated account keeps all its data but cannot log in
    public bool IsActive { get; private set; } = true;

    // Contact details of accounts that have no student or teacher record (administrators, managers);
    // the phone number is the one built into ASP.NET Identity
    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }

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

    public void SetContact(string? firstName, string? lastName, string? phoneNumber)
    {
        FirstName = string.IsNullOrWhiteSpace(firstName) ? null : firstName.Trim();
        LastName = string.IsNullOrWhiteSpace(lastName) ? null : lastName.Trim();
        PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();
        UpdatedAt = DateTime.UtcNow;
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
