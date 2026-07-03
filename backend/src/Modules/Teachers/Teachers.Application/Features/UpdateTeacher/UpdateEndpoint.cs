using FluentValidation;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Teachers.Application.Abstractions;

namespace Teachers.Application.Features.UpdateTeacher;

public sealed record UpdateTeacherRequest(
    string FirstName,
    string LastName,
    string? MiddleName,
    DateOnly DateOfBirth,
    string PhoneNumber,
    string City,
    string Country
);

public sealed class UpdateTeacherValidator : AbstractValidator<UpdateTeacherRequest>
{
    public UpdateTeacherValidator()
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

        RuleFor(x => x.City)
            .NotEmpty().WithMessage("City is required.")
            .MaximumLength(50).WithMessage("City must not exceed 50 characters.");

        RuleFor(x => x.Country)
            .NotEmpty().WithMessage("Country is required.")
            .MaximumLength(50).WithMessage("Country must not exceed 50 characters.");
    }
}

public static class UpdateEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPut("/{id:guid}", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanManageTeachers))
             .WithName("UpdateTeacher")
             .WithSummary("Update a teacher")
             .Produces(StatusCodes.Status204NoContent)
             .ProducesValidationProblem()
             .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        Guid id,
        UpdateTeacherRequest request,
        IValidator<UpdateTeacherRequest> validator,
        ITeacherRepository repository,
        ITeacherUnitOfWork unitOfWork,
        ILogger<UpdateTeacherRequest> logger,
        CancellationToken ct
    )
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
            return Results.ValidationProblem(validationResult.ToDictionary());

        var teacher = await repository.GetByIdAsync(id, ct);
        if (teacher is null)
            return Results.Problem(
                detail: $"Teacher with id '{id}' not found.",
                statusCode: StatusCodes.Status404NotFound
            );

        teacher.Update(
            request.FirstName,
            request.LastName,
            request.MiddleName,
            request.DateOfBirth,
            request.PhoneNumber,
            request.City,
            request.Country
        );

        await repository.UpdateAsync(teacher, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Teacher updated: {TeacherId}", id);

        return Results.NoContent();
    }
}
