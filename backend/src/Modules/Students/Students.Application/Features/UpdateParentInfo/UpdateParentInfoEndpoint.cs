using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Shared.Kernel.Abstractions;
using Students.Application.Abstractions;

namespace Students.Application.Features.UpdateParentInfo;

public sealed record UpdateParentInfoRequest(
    string FirstName,
    string LastName,
    string? MiddleName,
    string PhoneNumber,
    string Email
);

public sealed class UpdateParentInfoValidator : AbstractValidator<UpdateParentInfoRequest>
{
    public UpdateParentInfoValidator()
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

public static class UpdateParentInfoEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPut("/{id:guid}/parent-info", Handle)
             .WithName("UpdateParentInfo")
             .WithSummary("Update parent info for student")
             .Produces(StatusCodes.Status204NoContent)
             .ProducesValidationProblem()
             .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        Guid id,
        UpdateParentInfoRequest request,
        IValidator<UpdateParentInfoRequest> validator,
        IStudentRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateParentInfoRequest> logger,
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

        if (student.ParentInfo is null)
        {
            logger.LogWarning("Parent info not found for student {StudentId}", id);

            return Results.Problem(
                detail: "Parent info not found.",
                statusCode: StatusCodes.Status404NotFound
            );
        }

        student.ParentInfo.Update(
            request.FirstName,
            request.LastName,
            request.MiddleName,
            request.PhoneNumber,
            request.Email
        );

        await repository.UpdateAsync(student, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Parent info updated for student {StudentId}", id);

        return Results.NoContent();
    }
}
