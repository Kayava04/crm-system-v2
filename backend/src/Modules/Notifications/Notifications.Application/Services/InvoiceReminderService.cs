using Billing.Contracts;
using Microsoft.Extensions.Logging;
using Notifications.Contracts;
using Students.Contracts;

namespace Notifications.Application.Services;

internal sealed record InvoiceReminderResult(int InvoicesFound, int Created, int SkippedAlreadySent, int SkippedNoAccount);

// Used by the admin endpoint and by the periodic job, so both always behave the same
internal sealed class InvoiceReminderService(
    IInvoiceReminderSource invoiceSource,
    IStudentLookup studentLookup,
    NotificationDispatcher dispatcher,
    ILogger<InvoiceReminderService> logger)
{
    public async Task<InvoiceReminderResult> SendAsync(int daysBeforeDue, bool includeOverdue, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var invoices = await invoiceSource.GetUnpaidDueUntilAsync(today.AddDays(daysBeforeDue), includeOverdue, ct);

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

        return new InvoiceReminderResult(invoices.Count, result.Created, result.Duplicates, noAccount);
    }
}
