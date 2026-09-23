using Identity.Contracts;
using FluentValidation;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Notifications.Application.Services;
using Notifications.Contracts;

namespace Notifications.Application.Features.BroadcastNotification;

public enum BroadcastAudience
{
    Students,
    Teachers,
    Admins,
    Everyone
}

public sealed record BroadcastNotificationRequest(
    BroadcastAudience Audience,
    string Subject,
    string Body
);

public sealed record BroadcastNotificationResponse(int Created);

public sealed class BroadcastNotificationValidator : AbstractValidator<BroadcastNotificationRequest>
{
    public BroadcastNotificationValidator()
    {
        RuleFor(x => x.Audience)
            .IsInEnum().WithMessage("Invalid audience.");

        RuleFor(x => x.Subject)
            .NotEmpty().WithMessage("Subject is required.")
            .MaximumLength(200).WithMessage("Subject must not exceed 200 characters.");

        RuleFor(x => x.Body)
            .NotEmpty().WithMessage("Body is required.")
            .MaximumLength(2000).WithMessage("Body must not exceed 2000 characters.");
    }
}

public static class BroadcastNotificationEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/broadcast", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanManageNotifications))
             .WithName("BroadcastNotification")
             .WithSummary("Send a message to every active user of a group (students, teachers, admins or everyone)")
             .Produces<BroadcastNotificationResponse>(StatusCodes.Status200OK)
             .ProducesValidationProblem();
    }

    private static async Task<IResult> Handle(
        BroadcastNotificationRequest request,
        IValidator<BroadcastNotificationRequest> validator,
        IUserDirectory userDirectory,
        NotificationDispatcher dispatcher,
        ILogger<BroadcastNotificationRequest> logger,
        CancellationToken ct
    )
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
            return Results.ValidationProblem(validationResult.ToDictionary());

        var roles = request.Audience switch
        {
            BroadcastAudience.Students => new[] { SystemRole.Student },
            BroadcastAudience.Teachers => new[] { SystemRole.Teacher },
            BroadcastAudience.Admins => new[] { SystemRole.Admin, SystemRole.SuperAdmin },
            _ => Enum.GetValues<SystemRole>()
        };

        var recipients = new HashSet<Guid>();

        foreach (var role in roles)
            recipients.UnionWith(await userDirectory.GetActiveUserIdsByRoleAsync(role, ct));

        var drafts = recipients
            .Select(id => new NotificationDraft(id, request.Subject.Trim(), request.Body.Trim()))
            .ToList();

        var result = await dispatcher.DispatchAsync(NotificationType.General, drafts, ct: ct);

        logger.LogInformation("Broadcast to {Audience}: {Count} notifications", request.Audience, result.Created);

        return Results.Ok(new BroadcastNotificationResponse(result.Created));
    }
}
