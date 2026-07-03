using Enrollments.Application.Abstractions;
using Enrollments.Domain.Enums;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Enrollments.Application.Features.GetEnrollmentById;

public sealed record EnrollmentDetailResponse(
    Guid Id,
    string EnrollmentNumber,
    Guid StudentId,
    Guid CourseId,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal CoursePrice,
    decimal? DiscountedPrice,
    decimal EffectivePrice,
    EnrollmentStatus Status,
    string? Comment,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public static class GetByIdEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/{id:guid}", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanViewEnrollments))
             .WithName("GetEnrollmentById")
             .WithSummary("Get enrollment by id")
             .Produces<EnrollmentDetailResponse>(StatusCodes.Status200OK)
             .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        Guid id,
        IEnrollmentRepository repository,
        CancellationToken ct
    )
    {
        var enrollment = await repository.GetByIdAsync(id, ct);
        if (enrollment is null)
            return Results.Problem(
                detail: $"Enrollment with id '{id}' not found.",
                statusCode: StatusCodes.Status404NotFound
            );

        var response = new EnrollmentDetailResponse(
            enrollment.Id,
            enrollment.EnrollmentNumber,
            enrollment.StudentId,
            enrollment.CourseId,
            enrollment.StartDate,
            enrollment.EndDate,
            enrollment.CoursePrice,
            enrollment.DiscountedPrice,
            enrollment.EffectivePrice,
            enrollment.Status,
            enrollment.Comment,
            enrollment.CreatedAt,
            enrollment.UpdatedAt
        );

        return Results.Ok(response);
    }
}
