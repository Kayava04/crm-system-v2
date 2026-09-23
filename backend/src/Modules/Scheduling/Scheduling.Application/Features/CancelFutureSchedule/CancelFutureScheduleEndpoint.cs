using FluentValidation;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Scheduling.Application.Abstractions;

namespace Scheduling.Application.Features.CancelFutureSchedule;

// Exactly one of EnrollmentId and GroupId must be set
public sealed record CancelFutureScheduleRequest(Guid? EnrollmentId, Guid? GroupId);

public sealed record CancelFutureScheduleResponse(int CancelledCount);

public sealed class CancelFutureScheduleValidator : AbstractValidator<CancelFutureScheduleRequest>
{
    public CancelFutureScheduleValidator()
    {
        RuleFor(x => x)
            .Must(x => x.EnrollmentId is null != x.GroupId is null)
            .WithName("EnrollmentId")
            .WithMessage("Specify either an enrollment or a study group.");
    }
}

public static class CancelFutureScheduleEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPut("/cancel-future", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanManageSchedule))
             .WithName("CancelFutureSchedule")
             .WithSummary("Cancel all upcoming lessons of an enrollment or a group (before regenerating the schedule)")
             .Produces<CancelFutureScheduleResponse>(StatusCodes.Status200OK)
             .ProducesValidationProblem();
    }

    private static async Task<IResult> Handle(
        CancelFutureScheduleRequest request,
        IValidator<CancelFutureScheduleRequest> validator,
        IScheduleRepository repository,
        ISchedulingUnitOfWork unitOfWork,
        ILogger<CancelFutureScheduleRequest> logger,
        CancellationToken ct
    )
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
            return Results.ValidationProblem(validationResult.ToDictionary());

        var upcoming = await repository.GetFutureOpenAsync(
            request.EnrollmentId, request.GroupId, DateTime.UtcNow, ct);

        foreach (var schedule in upcoming)
            schedule.Cancel();

        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Cancelled {Count} upcoming lessons for Enrollment {EnrollmentId} / Group {GroupId}",
            upcoming.Count, request.EnrollmentId, request.GroupId
        );

        return Results.Ok(new CancelFutureScheduleResponse(upcoming.Count));
    }
}
