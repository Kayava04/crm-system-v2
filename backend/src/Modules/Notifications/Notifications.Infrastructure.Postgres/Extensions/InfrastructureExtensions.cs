using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notifications.Application.Abstractions;
using Notifications.Infrastructure.Postgres.Persistence;
using Notifications.Infrastructure.Postgres.Repositories;

namespace Notifications.Infrastructure.Postgres.Extensions;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddNotificationsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services
            .AddDatabase(configuration)
            .AddRepositories();

        return services;
    }

    private static IServiceCollection AddDatabase(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "Connection string 'Default' not found in configuration.");

        services.AddDbContext<NotificationsDbContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<INotificationsUnitOfWork>(sp => sp.GetRequiredService<NotificationsDbContext>());

        return services;
    }

    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<INotificationRepository, NotificationRepository>();

        return services;
    }
}
