using FluentValidation;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Students.Application.Abstractions;
using Students.Domain.Entities;
using Students.Domain.Enums;

namespace Students.Application.Features.CreateStudent;

public sealed record CreateRequest(
    string FirstName,
    string LastName,
    string? MiddleName,
    DateOnly DateOfBirth,
    string PhoneNumber,
    string Email,
    string City,
    string Country,
    bool IsChild,
    string? Comment,

    LearningGoal LearningGoal,
    Format Format,
    LessonType LessonType,
    int Intensity,
    Level CurrentLevel,
    bool HadPreviousCourses,

    List<Language> Languages
);

public sealed record CreateResponse(
    Guid Id,
    string FullName,
    string Email
);

public sealed class CreateValidator : AbstractValidator<CreateRequest>
{
    public CreateValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(50).WithMessage("First name must not exceed 50 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(50).WithMessage("Last name must not exceed 50 characters.");

        RuleFor(x => x.MiddleName)
            .MaximumLength(50).WithMessage("Middle name must not exceed 50 characters.")
            .When(x => x.MiddleName is not null);

        RuleFor(x => x.DateOfBirth)
            .NotEmpty().WithMessage("Date of birth is required.")
            .LessThan(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Invalid date of birth.");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required.")
            .Matches(@"^\+?[0-9\s\-\(\)]{7,20}$")
            .WithMessage("Invalid phone number format.");

        RuleFor(x => x.Comment)
            .MaximumLength(500).WithMessage("Comment must not exceed 500 characters.")
            .When(x => x.Comment is not null);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.");

        RuleFor(x => x.City)
            .NotEmpty().WithMessage("City is required.")
            .MaximumLength(50).WithMessage("City must not exceed 50 characters.");

        RuleFor(x => x.Country)
            .NotEmpty().WithMessage("Country is required.")
            .MaximumLength(50).WithMessage("Country must not exceed 50 characters.");

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

public static class CreateEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/", Handle)
             .WithName("CreateStudent")
             .RequireAuthorization(nameof(SystemPermission.CanCreateStudents))
             .WithSummary("Create a student")
             .Produces<CreateResponse>(StatusCodes.Status201Created)
             .ProducesValidationProblem()
             .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> Handle(
        CreateRequest request,
        IValidator<CreateRequest> validator,
        IStudentRepository repository,
        IStudentUnitOfWork unitOfWork,
        ILogger<CreateRequest> logger,
        CancellationToken ct
    )
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
            return Results.ValidationProblem(validationResult.ToDictionary());

        var emailExists = await repository.ExistsByEmailAsync(request.Email, ct);
        if (emailExists)
            return Results.Problem(
                detail: $"Student with email '{request.Email}' already exists.",
                statusCode: StatusCodes.Status409Conflict
            );

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

        var preferences = StudentPreferences.Create(
            student.Id,
            request.LearningGoal,
            request.Format,
            request.LessonType,
            request.Intensity,
            request.CurrentLevel,
            request.HadPreviousCourses
        );

        student.SetPreferences(preferences);

        foreach (var language in request.Languages)
            student.AddLanguage(language);

        await repository.AddAsync(student, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Student created: {StudentId}", student.Id);

        var response = new CreateResponse(
            student.Id,
            $"{student.FirstName} {student.LastName}",
            student.Email
        );

        return Results.Created($"/api/students/{response.Id}", response);
    }
}
