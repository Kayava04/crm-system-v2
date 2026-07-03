using FluentValidation;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Teachers.Application.Abstractions;

namespace Teachers.Application.Features.UpdateComment;

public sealed record UpdateCommentRequest(string? Comment);

public sealed class UpdateCommentValidator : AbstractValidator<UpdateCommentRequest>
{
    public UpdateCommentValidator()
    {
        RuleFor(x => x.Comment)
            .MaximumLength(500).WithMessage("Comment must not exceed 500 characters.")
            .When(x => x.Comment is not null);
    }
}

public static class UpdateCommentEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPatch("/{id:guid}/comment", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanManageTeachers))
             .WithName("UpdateTeacherComment")
             .WithSummary("Update teacher comment")
             .Produces(StatusCodes.Status204NoContent)
             .ProducesValidationProblem()
             .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        Guid id,
        UpdateCommentRequest request,
        IValidator<UpdateCommentRequest> validator,
        ITeacherRepository repository,
        ITeacherUnitOfWork unitOfWork,
        ILogger<UpdateCommentRequest> logger,
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

        teacher.UpdateComment(request.Comment);

        await repository.UpdateAsync(teacher, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Teacher {TeacherId} comment updated", id);

        return Results.NoContent();
    }
}
