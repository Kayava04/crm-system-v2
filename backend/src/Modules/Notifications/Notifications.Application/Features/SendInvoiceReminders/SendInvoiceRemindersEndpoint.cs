using FluentValidation;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Notifications.Application.Services;
using Notifications.Contracts;

namespace Notifications.Application.Features.SendInvoiceReminders;

public sealed record SendInvoiceRemindersRequest(
    int DaysBeforeDue = 3,
    bool IncludeOverdue = true
);

public sealed record SendInvoiceRemindersResponse(
    int InvoicesFound,
    int Created,
    int SkippedAlreadySent,
    int SkippedNoAccount
);

public sealed class SendInvoiceRemindersValidator : AbstractValidator<SendInvoiceRemindersRequest>
{
    public SendInvoiceRemindersValidator()
    {
        RuleFor(x => x.DaysBeforeDue)
            .InclusiveBetween(0, 60).WithMessage("Days before due date must be between 0 and 60.");
    }
}

public static class SendInvoiceRemindersEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/invoice-reminders", Handle)
             .RequireAuthorization(nameof(SystemPermission.CanManageNotifications))
             .WithName("SendInvoiceReminders")
             .WithSummary("Remind students about unpaid invoices that are due soon or overdue (each reminder is sent once)")
             .Produces<SendInvoiceRemindersResponse>(StatusCodes.Status200OK)
             .ProducesValidationProblem();
    }

    private static async Task<IResult> Handle(
        SendInvoiceRemindersRequest request,
        IValidator<SendInvoiceRemindersRequest> validator,
        InvoiceReminderService service,
        CancellationToken ct
    )
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
            return Results.ValidationProblem(validationResult.ToDictionary());

        var result = await service.SendAsync(request.DaysBeforeDue, request.IncludeOverdue, ct);

        return Results.Ok(new SendInvoiceRemindersResponse(
            result.InvoicesFound, result.Created, result.SkippedAlreadySent, result.SkippedNoAccount));
    }
}
