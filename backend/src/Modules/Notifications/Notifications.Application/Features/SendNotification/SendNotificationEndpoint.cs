using FluentValidation;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Notifications.Application.Services;
using Notifications.Contracts;

namespace Notifications.Application.Features.SendNotification;

public sealed record SendNotificationRequest(
    List<Guid> RecipientUserIds,
    string Subject,
    string Body
);

public sealed record SendNotificationResponse(int Created);

public sealed class SendNotificationValidator : AbstractValidator<SendNotificationRequest>
{
    public SendNotificationValidator()
    {
        RuleFor(x => x.RecipientUserIds)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("At least one recipient is required.")
            .Must(ids => ids.Count <= 1000).WithMessage("At most 1000 recipients per request.")
            .Must(ids => ids.All(id => id != Guid.Empty)).WithMessage("Recipient ids must not be empty.");

        RuleFor(x => x.Subject)
            .NotEmpty().WithMessage("Subject is required.")
            .MaximumLength(200).WithMessage("Subject must not exceed 200 characters.");

        RuleFor(x => x.Body)
            .NotEmpty().WithMessage("Body is required.")
            .MaximumLength(2000).WithMessage("Body must not exceed 2000 characters.");
    }
}

public static class SendNotificationEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/send", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanManageNotifications))
             .WithName("SendNotification")
             .WithSummary("Send a message to specific users")
             .Produces<SendNotificationResponse>(StatusCodes.Status200OK)
             .ProducesValidationProblem();
    }

    private static async Task<IResult> Handle(
        SendNotificationRequest request,
        IValidator<SendNotificationRequest> validator,
        NotificationDispatcher dispatcher,
        ILogger<SendNotificationRequest> logger,
        CancellationToken ct
    )
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
            return Results.ValidationProblem(validationResult.ToDictionary());

        var drafts = request.RecipientUserIds
            .Distinct()
            .Select(id => new NotificationDraft(id, request.Subject.Trim(), request.Body.Trim()))
            .ToList();

        var result = await dispatcher.DispatchAsync(NotificationType.General, drafts, ct: ct);

        logger.LogInformation("Notification sent to {Count} users", result.Created);

        return Results.Ok(new SendNotificationResponse(result.Created));
    }
}
