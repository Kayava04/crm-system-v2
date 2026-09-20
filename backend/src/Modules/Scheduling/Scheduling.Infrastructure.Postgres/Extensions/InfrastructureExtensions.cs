using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Scheduling.Application.Abstractions;
using Scheduling.Infrastructure.Postgres.Persistence;
using Scheduling.Infrastructure.Postgres.Repositories;

namespace Scheduling.Infrastructure.Postgres.Extensions;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddSchedulingInfrastructure(
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

        services.AddDbContext<SchedulingDbContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<ISchedulingUnitOfWork>(sp => sp.GetRequiredService<SchedulingDbContext>());

        return services;
    }

    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IScheduleRepository, ScheduleRepository>();

        return services;
    }
}
