using Shared.Kernel.Primitives;

namespace Students.Domain.Entities;

public sealed class ParentInfo : AuditableEntity
{
    public Guid StudentId { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string? MiddleName { get; private set; }
    public string PhoneNumber { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;

    private ParentInfo() { }

    public static ParentInfo Create(
        Guid studentId,
        string firstName,
        string lastName,
        string? middleName,
        string phoneNumber,
        string email
    )
    {
        return new ParentInfo
        {
            Id = Guid.NewGuid(),
            StudentId = studentId,
            FirstName = firstName,
            LastName = lastName,
            MiddleName = middleName,
            PhoneNumber = phoneNumber,
            Email = email,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(
        string firstName,
        string lastName,
        string? middleName,
        string phoneNumber,
        string email
    )
    {
        FirstName = firstName;
        LastName = lastName;
        MiddleName = middleName;
        PhoneNumber = phoneNumber;
        Email = email;
        UpdatedAt = DateTime.UtcNow;
    }
}
