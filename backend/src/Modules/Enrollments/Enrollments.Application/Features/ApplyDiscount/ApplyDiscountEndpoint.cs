using Enrollments.Application.Abstractions;
using FluentValidation;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace Enrollments.Application.Features.ApplyDiscount;

public sealed record ApplyDiscountRequest(decimal DiscountedPrice);

public sealed class ApplyDiscountValidator : AbstractValidator<ApplyDiscountRequest>
{
    public ApplyDiscountValidator()
    {
        RuleFor(x => x.DiscountedPrice)
            .GreaterThan(0).WithMessage("Discounted price must be greater than 0.");
    }
}

public static class ApplyDiscountEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPut("/{id:guid}/discount", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanManageEnrollments))
             .WithName("ApplyDiscount")
             .WithSummary("Apply discount to enrollment")
             .Produces(StatusCodes.Status204NoContent)
             .ProducesValidationProblem()
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> Handle(
        Guid id,
        ApplyDiscountRequest request,
        IValidator<ApplyDiscountRequest> validator,
        IEnrollmentRepository repository,
        IEnrollmentUnitOfWork unitOfWork,
        ILogger<ApplyDiscountRequest> logger,
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

        if (request.DiscountedPrice >= enrollment.CoursePrice)
        {
            logger.LogWarning(
                "Discounted price {DiscountedPrice} is not less than course price {CoursePrice}",
                request.DiscountedPrice, enrollment.CoursePrice
            );

            return Results.Problem(
                detail: "Discounted price must be less than the course price.",
                statusCode: StatusCodes.Status409Conflict
            );
        }

        enrollment.ApplyDiscount(request.DiscountedPrice);

        await repository.UpdateAsync(enrollment, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Discount applied to enrollment {EnrollmentId}", id);

        return Results.NoContent();
    }
}
