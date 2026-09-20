using Materials.Application.Abstractions;
using Materials.Infrastructure.Postgres.Persistence;
using Materials.Infrastructure.Postgres.Repositories;
using Shared.Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Materials.Infrastructure.Postgres.Extensions;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddMaterialsInfrastructure(
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
services.AddModuleDbContext<MaterialsDbContext>();

        services.AddScoped<IMaterialsUnitOfWork>(sp => sp.GetRequiredService<MaterialsDbContext>());

        return services;
    }

    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IMaterialRepository, MaterialRepository>();

        return services;
    }
}
