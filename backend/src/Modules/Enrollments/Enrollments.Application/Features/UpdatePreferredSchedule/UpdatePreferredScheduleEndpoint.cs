using Enrollments.Application.Abstractions;
using FluentValidation;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace Enrollments.Application.Features.UpdatePreferredSchedule;

public sealed record UpdatePreferredScheduleRequest(string? PreferredSchedule);

public sealed class UpdatePreferredScheduleValidator : AbstractValidator<UpdatePreferredScheduleRequest>
{
    public UpdatePreferredScheduleValidator()
    {
        RuleFor(x => x.PreferredSchedule)
            .MaximumLength(500).WithMessage("Preferred schedule must not exceed 500 characters.")
            .When(x => x.PreferredSchedule is not null);
    }
}

public static class UpdatePreferredScheduleEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPatch("/{id:guid}/preferred-schedule", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanManageEnrollments))
             .WithName("UpdateEnrollmentPreferredSchedule")
             .WithSummary("Update the time the student prefers to attend")
             .Produces(StatusCodes.Status204NoContent)
             .ProducesValidationProblem()
             .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        Guid id,
        UpdatePreferredScheduleRequest request,
        IValidator<UpdatePreferredScheduleRequest> validator,
        IEnrollmentRepository repository,
        IEnrollmentUnitOfWork unitOfWork,
        ILogger<UpdatePreferredScheduleRequest> logger,
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

        enrollment.UpdatePreferredSchedule(request.PreferredSchedule?.Trim());

        await repository.UpdateAsync(enrollment, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Enrollment {EnrollmentId} preferred schedule updated", id);

        return Results.NoContent();
    }
}
