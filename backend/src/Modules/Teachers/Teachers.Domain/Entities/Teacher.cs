using Shared.Kernel.Primitives;
using Teachers.Domain.Enums;

namespace Teachers.Domain.Entities;

public sealed class Teacher : AuditableEntity
{
    public Guid? UserId { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string? MiddleName { get; private set; }
    public DateOnly DateOfBirth { get; private set; }
    public string PhoneNumber { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public string Country { get; private set; } = string.Empty;
    public TeacherStatus Status { get; private set; }
    public string? Comment { get; private set; }

    public IReadOnlyCollection<TeacherSalaryRate> SalaryRates => _salaryRates.AsReadOnly();
    private readonly List<TeacherSalaryRate> _salaryRates = [];

    private Teacher() { }

    public static Teacher Create(
        string firstName,
        string lastName,
        string? middleName,
        DateOnly dateOfBirth,
        string phoneNumber,
        string email,
        string city,
        string country,
        string? comment = null
    )
    {
        return new Teacher
        {
            Id = Guid.NewGuid(),
            UserId = null,
            FirstName = firstName,
            LastName = lastName,
            MiddleName = middleName,
            DateOfBirth = dateOfBirth,
            PhoneNumber = phoneNumber,
            Email = email,
            City = city,
            Country = country,
            Status = TeacherStatus.Probation,
            Comment = comment,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(
        string firstName,
        string lastName,
        string? middleName,
        DateOnly dateOfBirth,
        string phoneNumber,
        string city,
        string country
    )
    {
        FirstName = firstName;
        LastName = lastName;
        MiddleName = middleName;
        DateOfBirth = dateOfBirth;
        PhoneNumber = phoneNumber;
        City = city;
        Country = country;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangeStatus(TeacherStatus status)
    {
        Status = status;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateComment(string? comment)
    {
        Comment = comment;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddSalaryRate(TeacherSalaryRate salaryRate)
    {
        _salaryRates.Add(salaryRate);
    }

    public void LinkUserAccount(Guid userId)
    {
        UserId = userId;
    }

    public TeacherSalaryRate? CurrentSalaryRate =>
        _salaryRates
            .Where(sr => sr.EffectiveFrom <= DateTime.UtcNow)
            .OrderByDescending(sr => sr.EffectiveFrom)
            .FirstOrDefault();
}
