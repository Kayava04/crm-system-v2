using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Notifications.Application.Features.GetMyNotifications;
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

        return app;
    }
}
