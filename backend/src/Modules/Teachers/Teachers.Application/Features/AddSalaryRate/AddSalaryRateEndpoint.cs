using FluentValidation;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Teachers.Application.Abstractions;
using Teachers.Domain.Entities;

namespace Teachers.Application.Features.AddSalaryRate;

public sealed record AddSalaryRateRequest(
    decimal BaseSalary,
    decimal LessonsRate,
    DateTime EffectiveFrom
);

public sealed class AddSalaryRateValidator : AbstractValidator<AddSalaryRateRequest>
{
    public AddSalaryRateValidator()
    {
        RuleFor(x => x.BaseSalary)
            .GreaterThan(0).WithMessage("Base salary must be greater than 0.");

        RuleFor(x => x.LessonsRate)
            .GreaterThan(0).WithMessage("Lessons rate must be greater than 0.");

        RuleFor(x => x.EffectiveFrom)
            .NotEmpty().WithMessage("Effective from date is required.")
            .GreaterThanOrEqualTo(DateTime.UtcNow.Date)
            .WithMessage("Effective from date cannot be in the past.");
    }
}

public static class AddSalaryRateEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/{id:guid}/salary-rates", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanManageTeachers))
             .WithName("AddSalaryRate")
             .WithSummary("Add a new salary rate for a teacher")
             .Produces(StatusCodes.Status204NoContent)
             .ProducesValidationProblem()
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> Handle(
        Guid id,
        AddSalaryRateRequest request,
        IValidator<AddSalaryRateRequest> validator,
        ITeacherRepository repository,
        ITeacherUnitOfWork unitOfWork,
        ILogger<AddSalaryRateRequest> logger,
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

        var duplicateRate = teacher.SalaryRates
            .Any(sr => sr.EffectiveFrom.Date == request.EffectiveFrom.Date);

        if (duplicateRate)
        {
            logger.LogWarning("Teacher {TeacherId} already has a salary rate effective from {EffectiveFrom}", id, request.EffectiveFrom);

            return Results.Problem(
                detail: $"Teacher already has a salary rate effective from '{request.EffectiveFrom:yyyy-MM-dd}'.",
                statusCode: StatusCodes.Status409Conflict
            );
        }

        var salaryRate = TeacherSalaryRate.Create(
            teacher.Id,
            request.BaseSalary,
            request.LessonsRate,
            request.EffectiveFrom
        );

        teacher.AddSalaryRate(salaryRate);

        await repository.UpdateAsync(teacher, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Salary rate added for teacher {TeacherId}, effective from {EffectiveFrom}", id, request.EffectiveFrom);

        return Results.NoContent();
    }
}
