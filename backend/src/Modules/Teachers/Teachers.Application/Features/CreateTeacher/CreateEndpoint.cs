using FluentValidation;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Teachers.Application.Abstractions;
using Teachers.Application.Services;
using Teachers.Domain.Entities;

namespace Teachers.Application.Features.CreateTeacher;

public sealed record CreateTeacherRequest(
    string FirstName,
    string LastName,
    string? MiddleName,
    DateOnly DateOfBirth,
    string PhoneNumber,
    string Email,
    string City,
    string Country,
    decimal BaseSalary,
    decimal LessonsRate,
    string? Comment
);

public sealed record CreateTeacherResponse(
    Guid Id,
    string FullName,
    string Email
);

public sealed class CreateTeacherValidator : AbstractValidator<CreateTeacherRequest>
{
    public CreateTeacherValidator()
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

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.");

        RuleFor(x => x.City)
            .NotEmpty().WithMessage("City is required.")
            .MaximumLength(50).WithMessage("City must not exceed 50 characters.");

        RuleFor(x => x.Country)
            .NotEmpty().WithMessage("Country is required.")
            .MaximumLength(50).WithMessage("Country must not exceed 50 characters.");

        RuleFor(x => x.BaseSalary)
            .GreaterThan(0).WithMessage("Base salary must be greater than 0.");

        RuleFor(x => x.LessonsRate)
            .GreaterThan(0).WithMessage("Lessons rate must be greater than 0.");

        RuleFor(x => x.Comment)
            .MaximumLength(500).WithMessage("Comment must not exceed 500 characters.")
            .When(x => x.Comment is not null);
    }
}

public static class CreateEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanCreateTeachers))
             .WithName("CreateTeacher")
             .WithSummary("Create a teacher")
             .Produces<CreateTeacherResponse>(StatusCodes.Status201Created)
             .ProducesValidationProblem()
             .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> Handle(
        CreateTeacherRequest request,
        IValidator<CreateTeacherRequest> validator,
        ITeacherRepository repository,
        ITeacherUnitOfWork unitOfWork,
        ILogger<CreateTeacherRequest> logger,
        CancellationToken ct
    )
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
            return Results.ValidationProblem(validationResult.ToDictionary());

        var emailExists = await repository.ExistsByEmailAsync(request.Email, ct);
        if (emailExists)
            return Results.Problem(
                detail: $"Teacher with email '{request.Email}' already exists.",
                statusCode: StatusCodes.Status409Conflict
            );

        var teacher = TeacherFactory.Build(request);

        await repository.AddAsync(teacher, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Teacher created: {TeacherId}", teacher.Id);

        var response = new CreateTeacherResponse(
            teacher.Id,
            $"{teacher.FirstName} {teacher.LastName}",
            teacher.Email
        );

        return Results.Created($"/api/teachers/{teacher.Id}", response);
    }
}
