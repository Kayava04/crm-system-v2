using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Shared.Kernel.Abstractions;
using Students.Application.Abstractions;
using Students.Domain.Entities;

namespace Students.Application.Features.AddParentInfo;

public sealed record AddParentInfoRequest(
    string FirstName,
    string LastName,
    string? MiddleName,
    string PhoneNumber,
    string Email
);

public sealed class AddParentInfoValidator : AbstractValidator<AddParentInfoRequest>
{
    public AddParentInfoValidator()
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

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required.")
            .Matches(@"^\+?[0-9\s\-\(\)]{7,20}$")
            .WithMessage("Invalid phone number format.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.");
    }
}

public static class AddParentInfoEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/{id:guid}/parent-info", Handle)
             .WithName("AddParentInfo")
             .WithSummary("Add parent info for student")
             .Produces(StatusCodes.Status201Created)
             .ProducesValidationProblem()
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> Handle(
        Guid id,
        AddParentInfoRequest request,
        IValidator<AddParentInfoRequest> validator,
        IStudentRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<AddParentInfoRequest> logger,
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

        if (!student.IsChild)
        {
            logger.LogWarning("Cannot add parent info for adult student {StudentId}", id);

            return Results.Problem(
                detail: "Cannot add parent info for adult student.",
                statusCode: StatusCodes.Status409Conflict
            );
        }

        if (student.ParentInfo is not null)
        {
            logger.LogWarning("Parent info already exists for student {StudentId}", id);

            return Results.Problem(
                detail: "Parent info already exists.",
                statusCode: StatusCodes.Status409Conflict
            );
        }

        var parentInfo = ParentInfo.Create(
            student.Id,
            request.FirstName,
            request.LastName,
            request.MiddleName,
            request.PhoneNumber,
            request.Email
        );

        student.SetParentInfo(parentInfo);

        await repository.UpdateAsync(student, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Parent info added for student {StudentId}", id);

        return Results.Created($"/api/students/{id}/parent-info", null);
    }
}
