using Education.Contracts.Enums;

namespace Students.Domain.Entities;

public sealed class StudentLanguage
{
    public Guid StudentId { get; private set; }
    public Language Language { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private StudentLanguage() { }

    public static StudentLanguage Create(Guid studentId, Language language)
    {
        return new StudentLanguage
        {
            StudentId = studentId,
            Language = language,
            CreatedAt = DateTime.UtcNow
        };
    }
}
