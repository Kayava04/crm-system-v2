using Teachers.Application.Features.CreateTeacher;
using Teachers.Domain.Entities;

namespace Teachers.Application.Services;

// The single place that turns a create request into a Teacher (with the first salary rate)
internal static class TeacherFactory
{
    public static Teacher Build(CreateTeacherRequest request)
    {
        var teacher = Teacher.Create(
            request.FirstName,
            request.LastName,
            request.MiddleName,
            request.DateOfBirth,
            request.PhoneNumber,
            request.Email,
            request.City,
            request.Country,
            request.Comment
        );

        teacher.AddSalaryRate(TeacherSalaryRate.Create(
            teacher.Id,
            request.BaseSalary,
            request.LessonsRate,
            DateTime.UtcNow
        ));

        return teacher;
    }
}
