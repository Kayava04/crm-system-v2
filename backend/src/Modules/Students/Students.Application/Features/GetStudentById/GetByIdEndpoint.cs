using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Students.Application.Abstractions;
using Students.Domain.Enums;

namespace Students.Application.Features.GetStudentById;

public sealed record ParentInfoResponse(
    string FirstName,
    string LastName,
    string? MiddleName,
    string PhoneNumber,
    string Email
);

public sealed record StudentPreferencesResponse(
    LearningGoal LearningGoal,
    Format Format,
    LessonType LessonType,
    int Intensity,
    Level CurrentLevel,
    bool HadPreviousCourses
);

public sealed record StudentDetailResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string? MiddleName,
    DateOnly DateOfBirth,
    string PhoneNumber,
    string Email,
    string City,
    string Country,
    bool IsChild,
    StudentPreferencesResponse Preferences,
    IReadOnlyList<Language> Languages,
    ParentInfoResponse? ParentInfo
);

public static class GetByIdEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/{id:guid}", Handle)
             .WithName("GetStudentById")
             .WithSummary("Get student by id")
             .Produces<StudentDetailResponse>(StatusCodes.Status200OK)
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    private static async Task<IResult> Handle(
        Guid id,
        IStudentRepository repository,
        ILogger<StudentDetailResponse> logger,
        CancellationToken ct
    )
    {
        var student = await repository.GetByIdAsync(id, ct);

        if (student is null)
            return Results.Problem(
                detail: $"Student with id '{id}' not found.",
                statusCode: StatusCodes.Status404NotFound
            );

        if (student.Preferences is null)
        {
            logger.LogError("Student {StudentId} has no preferences — data integrity issue", id);

            return Results.Problem(
                detail: "Student preferences not found.",
                statusCode: StatusCodes.Status500InternalServerError
            );
        }

        var response = new StudentDetailResponse(
            student.Id,
            student.FirstName,
            student.LastName,
            student.MiddleName,
            student.DateOfBirth,
            student.PhoneNumber,
            student.Email,
            student.City,
            student.Country,
            student.IsChild,

            new StudentPreferencesResponse(
                student.Preferences.LearningGoal,
                student.Preferences.Format,
                student.Preferences.LessonType,
                student.Preferences.Intensity,
                student.Preferences.CurrentLevel,
                student.Preferences.HadPreviousCourses
            ),

            student.Languages.Select(l => l.Language).ToList(),

            student.IsChild && student.ParentInfo is not null
                ? new ParentInfoResponse(
                    student.ParentInfo.FirstName,
                    student.ParentInfo.LastName,
                    student.ParentInfo.MiddleName,
                    student.ParentInfo.PhoneNumber,
                    student.ParentInfo.Email
                )
                : null
        );

        return Results.Ok(response);
    }
}
