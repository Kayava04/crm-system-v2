using Billing.Contracts;
using FluentValidation;
using Identity.Contracts.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Notifications.Application.Services;
using Notifications.Contracts;
using Students.Contracts;

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
        IInvoiceReminderSource invoiceSource,
        IStudentLookup studentLookup,
        NotificationDispatcher dispatcher,
        ILogger<SendInvoiceRemindersRequest> logger,
        CancellationToken ct
    )
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
            return Results.ValidationProblem(validationResult.ToDictionary());

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var invoices = await invoiceSource.GetUnpaidDueUntilAsync(
            today.AddDays(request.DaysBeforeDue), request.IncludeOverdue, ct);

        var students = (await studentLookup.GetByIdsAsync(
                invoices.Select(i => i.StudentId).Distinct().ToList(), ct))
            .ToDictionary(s => s.Id);

        var drafts = new List<NotificationDraft>();
        var noAccount = 0;

        foreach (var invoice in invoices)
        {
            if (!students.TryGetValue(invoice.StudentId, out var student) || student.UserId is not { } userId)
            {
                noAccount++;
                continue;
            }

            // One reminder per invoice and kind: "due soon" and "overdue" are separate messages
            var kind = invoice.IsOverdue ? "overdue" : "due";

            drafts.Add(new NotificationDraft(
                userId,
                invoice.IsOverdue ? $"Invoice for {invoice.Period} is overdue" : $"Invoice for {invoice.Period} is due soon",
                invoice.IsOverdue
                    ? $"Your invoice for {invoice.Period} ({invoice.Amount:0.##}) was due on {invoice.DueDate:yyyy-MM-dd} and is not paid yet."
                    : $"Your invoice for {invoice.Period} ({invoice.Amount:0.##}) is due on {invoice.DueDate:yyyy-MM-dd}.",
                $"invoice:{invoice.InvoiceId}:{kind}"));
        }

        var result = await dispatcher.DispatchAsync(NotificationType.InvoiceReminder, drafts, ct: ct);

        logger.LogInformation(
            "Invoice reminders: {Found} invoices, {Created} sent, {Duplicates} already sent, {NoAccount} without account",
            invoices.Count, result.Created, result.Duplicates, noAccount);

        return Results.Ok(new SendInvoiceRemindersResponse(invoices.Count, result.Created, result.Duplicates, noAccount));
    }
}
