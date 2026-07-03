using Courses.Application.Abstractions;
using Education.Contracts.Enums;
using FluentValidation;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace Courses.Application.Features.UpdateCourse;

public sealed record UpdateCourseRequest(
    string Name,
    Language Language,
    Level Level,
    Format Format,
    LessonType LessonType,
    int DurationMonths,
    int LessonsCount,
    int LessonsPerWeek,
    decimal Price,
    string? Description
);

public sealed class UpdateCourseValidator : AbstractValidator<UpdateCourseRequest>
{
    public UpdateCourseValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");

        RuleFor(x => x.Language)
            .IsInEnum().WithMessage("Invalid language.");

        RuleFor(x => x.Level)
            .IsInEnum().WithMessage("Invalid level.");

        RuleFor(x => x.Format)
            .IsInEnum().WithMessage("Invalid format.");

        RuleFor(x => x.LessonType)
            .IsInEnum().WithMessage("Invalid lesson type.");

        RuleFor(x => x.DurationMonths)
            .GreaterThan(0).WithMessage("Duration must be greater than 0.");

        RuleFor(x => x.LessonsCount)
            .GreaterThan(0).WithMessage("Lessons count must be greater than 0.");

        RuleFor(x => x.LessonsPerWeek)
            .InclusiveBetween(1, 7).WithMessage("Lessons per week must be between 1 and 7.");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than 0.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.")
            .When(x => x.Description is not null);
    }
}

public static class UpdateEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPut("/{id:guid}", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanManageCourses))
             .WithName("UpdateCourse")
             .WithSummary("Update a course")
             .Produces(StatusCodes.Status204NoContent)
             .ProducesValidationProblem()
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> Handle(
        Guid id,
        UpdateCourseRequest request,
        IValidator<UpdateCourseRequest> validator,
        ICourseRepository repository,
        ICoursesUnitOfWork unitOfWork,
        ILogger<UpdateCourseRequest> logger,
        CancellationToken ct
    )
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
            return Results.ValidationProblem(validationResult.ToDictionary());

        var course = await repository.GetByIdAsync(id, ct);
        if (course is null)
            return Results.Problem(
                detail: $"Course with id '{id}' not found.",
                statusCode: StatusCodes.Status404NotFound
            );

        var nameExists = await repository.ExistsByNameAsync(request.Name, ct);
        if (nameExists && course.Name.ToLower() != request.Name.ToLower())
            return Results.Problem(
                detail: $"Course with name '{request.Name}' already exists.",
                statusCode: StatusCodes.Status409Conflict
            );

        course.Update(
            request.Name,
            request.Language,
            request.Level,
            request.Format,
            request.LessonType,
            request.DurationMonths,
            request.LessonsCount,
            request.LessonsPerWeek,
            request.Price,
            request.Description
        );

        await repository.UpdateAsync(course, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Course updated: {CourseId}", id);

        return Results.NoContent();
    }
}
