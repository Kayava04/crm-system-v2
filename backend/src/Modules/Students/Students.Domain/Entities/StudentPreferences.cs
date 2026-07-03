using Education.Contracts.Enums;
using Shared.Kernel.Primitives;
using Students.Domain.Enums;

namespace Students.Domain.Entities;

public sealed class StudentPreferences : AuditableEntity
{
    public Guid StudentId { get; private set; }
    public LearningGoal LearningGoal { get; private set; }
    public Format Format { get; private set; }
    public LessonType LessonType { get; private set; }
    public int Intensity { get; private set; }
    public Level CurrentLevel { get; private set; }
    public bool HadPreviousCourses { get; private set; }

    private StudentPreferences() { }

    public static StudentPreferences Create(
        Guid studentId,
        LearningGoal learningGoal,
        Format format,
        LessonType lessonType,
        int intensity,
        Level currentLevel,
        bool hadPreviousCourses
    )
    {
        return new StudentPreferences
        {
            Id = Guid.NewGuid(),
            StudentId = studentId,
            LearningGoal = learningGoal,
            Format = format,
            LessonType = lessonType,
            Intensity = intensity,
            CurrentLevel = currentLevel,
            HadPreviousCourses = hadPreviousCourses,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(
        LearningGoal learningGoal,
        Format format,
        LessonType lessonType,
        int intensity,
        Level currentLevel,
        bool hadPreviousCourses
    )
    {
        LearningGoal = learningGoal;
        Format = format;
        LessonType = lessonType;
        Intensity = intensity;
        CurrentLevel = currentLevel;
        HadPreviousCourses = hadPreviousCourses;
        UpdatedAt = DateTime.UtcNow;
    }
}
