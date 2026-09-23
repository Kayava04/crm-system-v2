using Microsoft.AspNetCore.Identity;

namespace Identity.Domain.Entities;

public sealed class User : IdentityUser<Guid>
{
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool MustChangePassword { get; private set; }

    // A deactivated account keeps all its data but cannot log in
    public bool IsActive { get; private set; } = true;

    // Contact and personal details of accounts that have no student or teacher record (administrators,
    // managers); the phone number is the one built into ASP.NET Identity. A student or teacher account
    // keeps the same information in its own record instead (see UpdateMyContactEndpoint).
    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }
    public string? MiddleName { get; private set; }
    public DateOnly? DateOfBirth { get; private set; }
    public string? City { get; private set; }
    public string? Country { get; private set; }

    // The account's own salary, set by an administrator with CanManageAdmins (see StaffEndpoints); this
    // is separate from a teacher's payroll, which is generated from TeacherSalaryRate in the Billing module.
    public decimal? Salary { get; private set; }

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

    public void SetContact(
        string? firstName,
        string? lastName,
        string? phoneNumber,
        string? middleName = null,
        DateOnly? dateOfBirth = null,
        string? city = null,
        string? country = null)
    {
        FirstName = string.IsNullOrWhiteSpace(firstName) ? null : firstName.Trim();
        LastName = string.IsNullOrWhiteSpace(lastName) ? null : lastName.Trim();
        PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();
        MiddleName = string.IsNullOrWhiteSpace(middleName) ? null : middleName.Trim();
        DateOfBirth = dateOfBirth;
        City = string.IsNullOrWhiteSpace(city) ? null : city.Trim();
        Country = string.IsNullOrWhiteSpace(country) ? null : country.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    // Kept separate from SetContact: unlike the fields above, this is never self-reported
    public void SetSalary(decimal? salary)
    {
        Salary = salary;
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
