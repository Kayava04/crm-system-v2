using Students.Application.Features.CreateStudent;
using Students.Domain.Entities;

namespace Students.Application.Services;

// The single place that turns a create request into a Student, used by both single and bulk creation
internal static class StudentFactory
{
    public static Student Build(CreateRequest request)
    {
        var student = Student.Create(
            request.FirstName,
            request.LastName,
            request.MiddleName,
            request.DateOfBirth,
            request.PhoneNumber,
            request.Email,
            request.City,
            request.Country,
            request.IsChild,
            request.Comment
        );

        student.SetPreferences(StudentPreferences.Create(
            student.Id,
            request.LearningGoal,
            request.Format,
            request.LessonType,
            request.Intensity,
            request.CurrentLevel,
            request.HadPreviousCourses
        ));

        foreach (var language in request.Languages)
            student.AddLanguage(language);

        return student;
    }
}
