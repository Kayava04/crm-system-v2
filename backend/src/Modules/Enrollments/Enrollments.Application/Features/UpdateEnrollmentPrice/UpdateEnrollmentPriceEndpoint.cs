using Enrollments.Application.Abstractions;
using FluentValidation;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace Enrollments.Application.Features.UpdateEnrollmentPrice;

public sealed record UpdateEnrollmentPriceRequest(decimal CoursePrice);

public sealed class UpdateEnrollmentPriceValidator : AbstractValidator<UpdateEnrollmentPriceRequest>
{
    public UpdateEnrollmentPriceValidator()
    {
        RuleFor(x => x.CoursePrice)
            .GreaterThan(0).WithMessage("Course price must be greater than 0.");
    }
}

public static class UpdateEnrollmentPriceEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPut("/{id:guid}/price", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanManageEnrollments))
             .WithName("UpdateEnrollmentPrice")
             .WithSummary("Update enrollment price")
             .Produces(StatusCodes.Status204NoContent)
             .ProducesValidationProblem()
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> Handle(
        Guid id,
        UpdateEnrollmentPriceRequest request,
        IValidator<UpdateEnrollmentPriceRequest> validator,
        IEnrollmentRepository repository,
        IEnrollmentUnitOfWork unitOfWork,
        ILogger<UpdateEnrollmentPriceRequest> logger,
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

        if (enrollment.CoursePrice == request.CoursePrice)
        {
            logger.LogWarning("Enrollment {EnrollmentId} already has price {CoursePrice}", id, request.CoursePrice);

            return Results.Problem(
                detail: "Enrollment already has this price.",
                statusCode: StatusCodes.Status409Conflict
            );
        }

        enrollment.UpdatePrice(request.CoursePrice);

        await repository.UpdateAsync(enrollment, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Enrollment {EnrollmentId} price updated to {CoursePrice}", id, request.CoursePrice);

        return Results.NoContent();
    }
}
