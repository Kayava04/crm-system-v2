using FluentValidation;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Students.Application.Abstractions;
using Students.Domain.Enums;

namespace Students.Application.Features.ChangeStudentStatus;

public sealed record ChangeStudentStatusRequest(StudentStatus Status);

public sealed class ChangeStudentStatusValidator : AbstractValidator<ChangeStudentStatusRequest>
{
    public ChangeStudentStatusValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid student status.");
    }
}

public static class ChangeStudentStatusEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPut("/{id:guid}/status", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanManageStudents))
             .WithName("ChangeStudentStatus")
             .WithSummary("Change student status")
             .Produces(StatusCodes.Status204NoContent)
             .ProducesValidationProblem()
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> Handle(
        Guid id,
        ChangeStudentStatusRequest request,
        IValidator<ChangeStudentStatusRequest> validator,
        IStudentRepository repository,
        IStudentUnitOfWork unitOfWork,
        ILogger<ChangeStudentStatusRequest> logger,
        CancellationToken ct
    )
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
            return Results.ValidationProblem(validationResult.ToDictionary());

        var student = await repository.GetByIdAsync(id, ct);
        if (student is null)
            return Results.Problem(
                detail: $"Student with id '{id}' not found.",
                statusCode: StatusCodes.Status404NotFound
            );

        if (student.Status == request.Status)
        {
            logger.LogWarning("Student {StudentId} already has status {Status}", id, request.Status);

            return Results.Problem(
                detail: $"Student already has status '{request.Status}'.",
                statusCode: StatusCodes.Status409Conflict
            );
        }

        student.ChangeStatus(request.Status);

        await repository.UpdateAsync(student, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Student {StudentId} status changed to {Status}", id, request.Status);

        return Results.NoContent();
    }
}
