using Courses.Application.Abstractions;
using Courses.Domain.Entities;
using Education.Contracts.Enums;
using FluentValidation;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace Courses.Application.Features.CreateCourse;

public sealed record CreateCourseRequest(
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

public sealed record CreateCourseResponse(Guid Id, string Name);

public sealed class CreateCourseValidator : AbstractValidator<CreateCourseRequest>
{
    public CreateCourseValidator()
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

public static class CreateEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanManageCourses))
             .WithName("CreateCourse")
             .WithSummary("Create a course")
             .Produces<CreateCourseResponse>(StatusCodes.Status201Created)
             .ProducesValidationProblem()
             .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> Handle(
        CreateCourseRequest request,
        IValidator<CreateCourseRequest> validator,
        ICourseRepository repository,
        ICoursesUnitOfWork unitOfWork,
        ILogger<CreateCourseRequest> logger,
        CancellationToken ct
    )
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
            return Results.ValidationProblem(validationResult.ToDictionary());

        var nameExists = await repository.ExistsByNameAsync(request.Name, ct);
        if (nameExists)
            return Results.Problem(
                detail: $"Course with name '{request.Name}' already exists.",
                statusCode: StatusCodes.Status409Conflict
            );

        var course = Course.Create(
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

        await repository.AddAsync(course, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Course created: {CourseId}", course.Id);

        var response = new CreateCourseResponse(course.Id, course.Name);

        return Results.Created($"/api/courses/{course.Id}", response);
    }
}
