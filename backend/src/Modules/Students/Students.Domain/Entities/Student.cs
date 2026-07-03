using Education.Contracts.Enums;
using Shared.Kernel.Primitives;
using Students.Domain.Enums;

namespace Students.Domain.Entities;

public sealed class Student : AuditableEntity
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
    public bool IsChild { get; private set; }
    public StudentStatus Status { get; private set; }
    public string? Comment { get; private set; }

    public StudentPreferences? Preferences { get; private set; }
    public ParentInfo? ParentInfo { get; private set; }
    public IReadOnlyCollection<StudentLanguage> Languages => _languages.AsReadOnly();
    private readonly List<StudentLanguage> _languages = [];

    private Student() { }

    public static Student Create(
        string firstName,
        string lastName,
        string? middleName,
        DateOnly dateOfBirth,
        string phoneNumber,
        string email,
        string city,
        string country,
        bool isChild,
        string? comment = null
    )
    {
        return new Student
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
            IsChild = isChild,
            Status = StudentStatus.Active,
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
        string country,
        bool isChild
    )
    {
        FirstName = firstName;
        LastName = lastName;
        MiddleName = middleName;
        DateOfBirth = dateOfBirth;
        PhoneNumber = phoneNumber;
        City = city;
        Country = country;
        IsChild = isChild;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangeStatus(StudentStatus status)
    {
        Status = status;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateComment(string? comment)
    {
        Comment = comment;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPreferences(StudentPreferences preferences)
    {
        Preferences = preferences;
    }

    public void AddLanguage(Language language)
    {
        if (_languages.Any(l => l.Language == language))
            return;

        _languages.Add(StudentLanguage.Create(Id, language));
    }

    public void UpdateLanguages(List<Language> languages)
    {
        _languages.Clear();

        foreach (var language in languages)
            _languages.Add(StudentLanguage.Create(Id, language));
    }

    public void SetParentInfo(ParentInfo parentInfo)
    {
        ParentInfo = parentInfo;
    }

    public void RemoveParentInfo()
    {
        ParentInfo = null;
    }

    public void LinkUserAccount(Guid userId)
    {
        UserId = userId;
    }
}
