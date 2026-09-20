using Shared.Infrastructure.Extensions;
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
services.AddModuleDbContext<SchedulingDbContext>();

        services.AddScoped<ISchedulingUnitOfWork>(sp => sp.GetRequiredService<SchedulingDbContext>());

        return services;
    }

    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IScheduleRepository, ScheduleRepository>();
        services.AddScoped<IStudyGroupRepository, StudyGroupRepository>();

        return services;
    }
}
