using FluentValidation;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Notifications.Application.Services;
using Notifications.Contracts;

namespace Notifications.Application.Features.SendLessonReminders;

public sealed record SendLessonRemindersRequest(int HoursAhead = 24);

public sealed record SendLessonRemindersResponse(
    int LessonsFound,
    int Created,
    int SkippedAlreadySent
);

public sealed class SendLessonRemindersValidator : AbstractValidator<SendLessonRemindersRequest>
{
    public SendLessonRemindersValidator()
    {
        RuleFor(x => x.HoursAhead)
            .InclusiveBetween(1, 168).WithMessage("Hours ahead must be between 1 and 168.");
    }
}

public static class SendLessonRemindersEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/lesson-reminders", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanManageNotifications))
             .WithName("SendLessonReminders")
             .WithSummary("Remind students and teachers about lessons that start soon (each reminder is sent once)")
             .Produces<SendLessonRemindersResponse>(StatusCodes.Status200OK)
             .ProducesValidationProblem();
    }

    private static async Task<IResult> Handle(
        SendLessonRemindersRequest request,
        IValidator<SendLessonRemindersRequest> validator,
        LessonReminderService service,
        CancellationToken ct
    )
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
            return Results.ValidationProblem(validationResult.ToDictionary());

        var result = await service.SendAsync(request.HoursAhead, ct);

        return Results.Ok(new SendLessonRemindersResponse(result.LessonsFound, result.Created, result.SkippedAlreadySent));
    }
}
