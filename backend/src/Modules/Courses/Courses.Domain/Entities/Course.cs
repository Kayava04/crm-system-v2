using Courses.Domain.Enums;
using Education.Contracts.Enums;
using Shared.Kernel.Primitives;

namespace Courses.Domain.Entities;

public sealed class Course : AuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public Language Language { get; private set; }
    public Level Level { get; private set; }
    public Format Format { get; private set; }
    public LessonType LessonType { get; private set; }
    public int DurationMonths { get; private set; }
    public int LessonsCount { get; private set; }
    public int LessonsPerWeek { get; private set; }
    public decimal Price { get; private set; }
    public string? Description { get; private set; }
    public CourseStatus Status { get; private set; }

    private Course() { }

    public static Course Create(
        string name,
        Language language,
        Level level,
        Format format,
        LessonType lessonType,
        int durationMonths,
        int lessonsCount,
        int lessonsPerWeek,
        decimal price,
        string? description = null
    )
    {
        return new Course
        {
            Id = Guid.NewGuid(),
            Name = name,
            Language = language,
            Level = level,
            Format = format,
            LessonType = lessonType,
            DurationMonths = durationMonths,
            LessonsCount = lessonsCount,
            LessonsPerWeek = lessonsPerWeek,
            Price = price,
            Description = description,
            Status = CourseStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(
        string name,
        Language language,
        Level level,
        Format format,
        LessonType lessonType,
        int durationMonths,
        int lessonsCount,
        int lessonsPerWeek,
        decimal price,
        string? description
    )
    {
        Name = name;
        Language = language;
        Level = level;
        Format = format;
        LessonType = lessonType;
        DurationMonths = durationMonths;
        LessonsCount = lessonsCount;
        LessonsPerWeek = lessonsPerWeek;
        Price = price;
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Archive()
    {
        Status = CourseStatus.Archived;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        Status = CourseStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }
}
