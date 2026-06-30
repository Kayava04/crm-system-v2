using FluentValidation;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Shared.Kernel.Abstractions;
using Students.Application.Abstractions;
using Students.Domain.Enums;

namespace Students.Application.Features.UpdateStudentPreferences;

public sealed record UpdatePreferencesRequest(
    LearningGoal LearningGoal,
    Format Format,
    LessonType LessonType,
    int Intensity,
    Level CurrentLevel,
    bool HadPreviousCourses,
    List<Language> Languages
);

public sealed class UpdatePreferencesValidator : AbstractValidator<UpdatePreferencesRequest>
{
    public UpdatePreferencesValidator()
    {
        RuleFor(x => x.LearningGoal)
            .IsInEnum().WithMessage("Invalid learning goal.");

        RuleFor(x => x.Format)
            .IsInEnum().WithMessage("Invalid format.");

        RuleFor(x => x.LessonType)
            .IsInEnum().WithMessage("Invalid lesson type.");

        RuleFor(x => x.Intensity)
            .InclusiveBetween(1, 7)
            .WithMessage("Intensity must be between 1 and 7 times per week.");

        RuleFor(x => x.CurrentLevel)
            .IsInEnum().WithMessage("Invalid language level.");

        RuleFor(x => x.Languages)
            .NotEmpty().WithMessage("At least one language is required.")
            .Must(l => l.Distinct().Count() == l.Count)
            .WithMessage("Languages must not contain duplicates.");

        RuleForEach(x => x.Languages)
            .IsInEnum().WithMessage("Invalid language.");
    }
}

public static class UpdatePreferencesEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPut("/{id:guid}/preferences", Handle)
             .WithName("UpdateStudentPreferences")
             .RequireAuthorization(nameof(SystemPermission.CanManageStudents))
             .WithSummary("Update student preferences")
             .Produces(StatusCodes.Status204NoContent)
             .ProducesValidationProblem()
             .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        Guid id,
        UpdatePreferencesRequest request,
        IValidator<UpdatePreferencesRequest> validator,
        IStudentRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<UpdatePreferencesRequest> logger,
        CancellationToken ct
    )
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
            return Results.ValidationProblem(validationResult.ToDictionary());

        var student = await repository.GetByIdAsync(id, ct);
        if (student is null)
        {
            logger.LogWarning("Student with id {StudentId} not found", id);

            return Results.Problem(
                detail: $"Student with id '{id}' not found.",
                statusCode: StatusCodes.Status404NotFound
            );
        }

        if (student.Preferences is null)
        {
            logger.LogError("Student {StudentId} has no preferences — data integrity issue", id);

            return Results.Problem(
                detail: "Student preferences not found.",
                statusCode: StatusCodes.Status500InternalServerError
            );
        }

        student.Preferences.Update(
            request.LearningGoal,
            request.Format,
            request.LessonType,
            request.Intensity,
            request.CurrentLevel,
            request.HadPreviousCourses
        );

        student.UpdateLanguages(request.Languages);

        await repository.UpdateAsync(student, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Student preferences updated: {StudentId}", id);

        return Results.NoContent();
    }
}
