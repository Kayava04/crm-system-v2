using Billing.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notifications.Contracts;
using Shared.Kernel.Abstractions;

namespace Notifications.Application.Services;

internal sealed class NotificationAutomationRunner(
    IServiceScopeFactory scopeFactory,
    IAdvisoryLock advisoryLock,
    IOptions<NotificationAutomationOptions> options,
    ILogger<NotificationAutomationRunner> logger
) : INotificationAutomation
{
    public async Task<AutomationRunResult> RunOnceAsync(CancellationToken ct = default)
    {
        // With several instances of the application only one runs the job at a time
        await using var handle = await advisoryLock.TryAcquireAsync(NotificationAutomationLock.Name, ct);

        if (handle is null)
        {
            logger.LogInformation("Notification automation skipped: another instance is running it");

            return new AutomationRunResult(true, 0, 0, 0, []);
        }

        var settings = options.Value;
        var errors = new List<string>();
        int overdue = 0, invoiceReminders = 0, lessonReminders = 0;

        if (settings.MarkOverdueInvoices)
            overdue = await StepAsync("mark overdue invoices", errors,
                (sp, token) => sp.GetRequiredService<IInvoiceOverdueMarker>().MarkOverdueAsync(token), ct);

        invoiceReminders = await StepAsync("invoice reminders", errors, async (sp, token) =>
            (await sp.GetRequiredService<InvoiceReminderService>()
                .SendAsync(settings.SafeInvoiceDays, settings.IncludeOverdue, token)).Created, ct);

        lessonReminders = await StepAsync("lesson reminders", errors, async (sp, token) =>
            (await sp.GetRequiredService<LessonReminderService>()
                .SendAsync(settings.SafeLessonHours, token)).Created, ct);

        logger.LogInformation(
            "Notification automation finished: {Overdue} invoices marked overdue, {InvoiceReminders} invoice and {LessonReminders} lesson reminders, {Errors} errors",
            overdue, invoiceReminders, lessonReminders, errors.Count);

        return new AutomationRunResult(false, overdue, invoiceReminders, lessonReminders, errors);
    }

    // Each step gets its own scope (and so its own DbContexts): a failure in one cannot spoil the next
    private async Task<int> StepAsync(
        string name,
        List<string> errors,
        Func<IServiceProvider, CancellationToken, Task<int>> step,
        CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();

            return await step(scope.ServiceProvider, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Notification automation step failed: {Step}", name);
            errors.Add($"{name}: {ex.Message}");

            return 0;
        }
    }
}
