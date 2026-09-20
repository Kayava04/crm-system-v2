using FluentValidation;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Scheduling.Application.Abstractions;
using Teachers.Contracts;

namespace Scheduling.Application.Features.UpdateGroup;

public sealed record UpdateGroupRequest(
    string Name,
    Guid TeacherId
);

public sealed class UpdateGroupValidator : AbstractValidator<UpdateGroupRequest>
{
    public UpdateGroupValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");

        RuleFor(x => x.TeacherId)
            .NotEmpty().WithMessage("Teacher is required.");
    }
}

public static class UpdateGroupEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPut("/{id:guid}", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanManageSchedule))
             .WithName("UpdateStudyGroup")
             .WithSummary("Update study group name and teacher")
             .Produces(StatusCodes.Status204NoContent)
             .ProducesValidationProblem()
             .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        Guid id,
        UpdateGroupRequest request,
        IValidator<UpdateGroupRequest> validator,
        IStudyGroupRepository repository,
        ISchedulingUnitOfWork unitOfWork,
        ITeacherVerifier teacherVerifier,
        ILogger<UpdateGroupRequest> logger,
        CancellationToken ct
    )
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
            return Results.ValidationProblem(validationResult.ToDictionary());

        var studyGroup = await repository.GetByIdAsync(id, ct);
        if (studyGroup is null)
            return Results.Problem(
                detail: $"Study group with id '{id}' not found.",
                statusCode: StatusCodes.Status404NotFound
            );

        if (request.TeacherId != studyGroup.TeacherId)
        {
            var teacherExists = await teacherVerifier.ExistsAsync(request.TeacherId, ct);
            if (!teacherExists)
                return Results.Problem(
                    detail: $"Teacher with id '{request.TeacherId}' not found.",
                    statusCode: StatusCodes.Status404NotFound
                );
        }

        if (request.TeacherId != studyGroup.TeacherId
            && !await teacherVerifier.IsAvailableAsync(request.TeacherId, ct))
            return Results.Problem(
                detail: "Teacher is not active and cannot lead a group.",
                statusCode: StatusCodes.Status409Conflict
            );

        studyGroup.Update(request.Name.Trim(), request.TeacherId);

        await repository.UpdateAsync(studyGroup, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Study group updated: {GroupId}", id);

        return Results.NoContent();
    }
}
