using Enrollments.Application.Abstractions;
using FluentValidation;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace Enrollments.Application.Features.UpdateEnrollmentComment;

public sealed record UpdateEnrollmentCommentRequest(string? Comment);

public sealed class UpdateEnrollmentCommentValidator : AbstractValidator<UpdateEnrollmentCommentRequest>
{
    public UpdateEnrollmentCommentValidator()
    {
        RuleFor(x => x.Comment)
            .MaximumLength(500).WithMessage("Comment must not exceed 500 characters.")
            .When(x => x.Comment is not null);
    }
}

public static class UpdateEnrollmentCommentEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPatch("/{id:guid}/comment", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanManageEnrollments))
             .WithName("UpdateEnrollmentComment")
             .WithSummary("Update enrollment comment")
             .Produces(StatusCodes.Status204NoContent)
             .ProducesValidationProblem()
             .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        Guid id,
        UpdateEnrollmentCommentRequest request,
        IValidator<UpdateEnrollmentCommentRequest> validator,
        IEnrollmentRepository repository,
        IEnrollmentUnitOfWork unitOfWork,
        ILogger<UpdateEnrollmentCommentRequest> logger,
        CancellationToken ct
    )
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
            return Results.ValidationProblem(validationResult.ToDictionary());

        var enrollment = await repository.GetByIdAsync(id, ct);
        if (enrollment is null)
            return Results.Problem(
                detail: $"Enrollment with id '{id}' not found.",
                statusCode: StatusCodes.Status404NotFound
            );

        enrollment.UpdateComment(request.Comment);

        await repository.UpdateAsync(enrollment, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Enrollment {EnrollmentId} comment updated", id);

        return Results.NoContent();
    }
}
