using Enrollments.Application.Abstractions;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace Enrollments.Application.Features.RemoveDiscount;

public sealed record RemoveDiscountRequest;

public static class RemoveDiscountEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapDelete("/{id:guid}/discount", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanManageEnrollments))
             .WithName("RemoveDiscount")
             .WithSummary("Remove discount from enrollment")
             .Produces(StatusCodes.Status204NoContent)
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> Handle(
        Guid id,
        IEnrollmentRepository repository,
        IEnrollmentUnitOfWork unitOfWork,
        ILogger<RemoveDiscountRequest> logger,
        CancellationToken ct
    )
    {
        var enrollment = await repository.GetByIdAsync(id, ct);
        if (enrollment is null)
            return Results.Problem(
                detail: $"Enrollment with id '{id}' not found.",
                statusCode: StatusCodes.Status404NotFound
            );

        if (enrollment.DiscountedPrice is null)
        {
            logger.LogWarning("Enrollment {EnrollmentId} has no discount to remove", id);

            return Results.Problem(
                detail: "Enrollment has no discount to remove.",
                statusCode: StatusCodes.Status409Conflict
            );
        }

        enrollment.RemoveDiscount();

        await repository.UpdateAsync(enrollment, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Discount removed from enrollment {EnrollmentId}", id);

        return Results.NoContent();
    }
}
