using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notifications.Contracts;

namespace Notifications.Application.Services;

// The timer of the periodic job; does nothing unless Notifications:Automation:Enabled is true
internal sealed class NotificationAutomationService(
    INotificationAutomation automation,
    IOptions<NotificationAutomationOptions> options,
    ILogger<NotificationAutomationService> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;

        if (!settings.Enabled)
        {
            logger.LogInformation("Notification automation is disabled");
            return;
        }

        logger.LogInformation("Notification automation started: every {Seconds} s", settings.SafeIntervalSeconds);

        try
        {
            // Let the start-up (migrations, seeding) finish before the first run
            await Task.Delay(TimeSpan.FromSeconds(settings.SafeStartupDelaySeconds), stoppingToken);

            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(settings.SafeIntervalSeconds));

            do
            {
                try
                {
                    await automation.RunOnceAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // e.g. the database is briefly unavailable: the next tick simply tries again
                    logger.LogError(ex, "Notification automation run failed");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // the application is stopping
        }
    }
}
