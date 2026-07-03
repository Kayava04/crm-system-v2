using FluentValidation;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Teachers.Application.Abstractions;
using Teachers.Domain.Enums;

namespace Teachers.Application.Features.ChangeTeacherStatus;

public sealed record ChangeTeacherStatusRequest(TeacherStatus Status);

public sealed class ChangeTeacherStatusValidator : AbstractValidator<ChangeTeacherStatusRequest>
{
    public ChangeTeacherStatusValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid teacher status.");
    }
}

public static class ChangeTeacherStatusEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPut("/{id:guid}/status", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanManageTeachers))
             .WithName("ChangeTeacherStatus")
             .WithSummary("Change teacher status")
             .Produces(StatusCodes.Status204NoContent)
             .ProducesValidationProblem()
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> Handle(
        Guid id,
        ChangeTeacherStatusRequest request,
        IValidator<ChangeTeacherStatusRequest> validator,
        ITeacherRepository repository,
        ITeacherUnitOfWork unitOfWork,
        ILogger<ChangeTeacherStatusRequest> logger,
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

        if (teacher.Status == request.Status)
        {
            logger.LogWarning("Teacher {TeacherId} already has status {Status}", id, request.Status);

            return Results.Problem(
                detail: $"Teacher already has status '{request.Status}'.",
                statusCode: StatusCodes.Status409Conflict
            );
        }

        teacher.ChangeStatus(request.Status);

        await repository.UpdateAsync(teacher, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Teacher {TeacherId} status changed to {Status}", id, request.Status);

        return Results.NoContent();
    }
}
