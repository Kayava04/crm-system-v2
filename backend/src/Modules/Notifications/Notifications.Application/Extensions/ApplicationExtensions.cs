using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notifications.Application.Features.BroadcastNotification;
using Notifications.Application.Features.GetAllNotifications;
using Notifications.Application.Features.GetMyNotifications;
using Notifications.Application.Features.SendInvoiceReminders;
using Notifications.Application.Features.SendLessonReminders;
using Notifications.Application.Features.SendNotification;
using Notifications.Application.Services;
using Notifications.Application.Features.GetUnreadCount;
using Notifications.Application.Features.MarkAllNotificationsRead;
using Notifications.Application.Features.MarkNotificationRead;
using Notifications.Contracts;

namespace Notifications.Application.Extensions;

public static class ApplicationExtensions
{
    public static IServiceCollection AddNotificationsApplication(this IServiceCollection services)
    {
        // Register FluentValidation
        services.AddValidatorsFromAssembly(typeof(ApplicationExtensions).Assembly);

        services.AddScoped<INotificationSender, NotificationSenderService>();
        services.AddScoped<NotificationDispatcher>();
        services.AddScoped<InvoiceReminderService>();
        services.AddScoped<LessonReminderService>();

        // The periodic job: the timer is a hosted service, the work itself is INotificationAutomation
        services.AddOptions<NotificationAutomationOptions>()
            .Configure<IConfiguration>((options, configuration) =>
                configuration.GetSection(NotificationAutomationOptions.SectionName).Bind(options));
        services.AddSingleton<INotificationAutomation, NotificationAutomationRunner>();
        services.AddHostedService<NotificationAutomationService>();

        return services;
    }

    public static IEndpointRouteBuilder MapNotificationsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/notifications")
                       .WithTags("Notifications");

        GetMyNotificationsEndpoint.Map(group);
        GetUnreadCountEndpoint.Map(group);
        MarkNotificationReadEndpoint.Map(group);
        MarkAllNotificationsReadEndpoint.Map(group);

        // Administration: only for users with CanManageNotifications
        GetAllNotificationsEndpoint.Map(group);
        SendNotificationEndpoint.Map(group);
        BroadcastNotificationEndpoint.Map(group);
        SendInvoiceRemindersEndpoint.Map(group);
        SendLessonRemindersEndpoint.Map(group);

        return app;
    }
}
