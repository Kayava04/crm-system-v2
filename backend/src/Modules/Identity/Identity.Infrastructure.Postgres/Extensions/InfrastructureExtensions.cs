using Identity.Application.Abstractions;
using Identity.Application.Seeding;
using Identity.Domain.Entities;
using Identity.Infrastructure.Postgres.Persistence;
using Identity.Infrastructure.Postgres.Repositories;
using Identity.Infrastructure.Postgres.Services;
using Shared.Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.Infrastructure.Postgres.Extensions;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddIdentityInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services
            .AddDatabase(configuration)
            .AddIdentityCore()
            .AddRepositories()
            .AddServices()
            .AddSeeding();

        return services;
    }

    private static IServiceCollection AddDatabase(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
services.AddModuleDbContext<IdentityDbContext>();

        services.AddScoped<IIdentityUnitOfWork>(sp => sp.GetRequiredService<IdentityDbContext>());

        return services;
    }

    private static IServiceCollection AddIdentityCore(this IServiceCollection services)
    {
        services
            .AddIdentityCore<User>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequiredLength = 8;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<IdentityDbContext>();

        return services;
    }

    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        return services;
    }

    private static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddScoped<IIdentityService, IdentityService>();

        return services;
    }

    private static IServiceCollection AddSeeding(this IServiceCollection services)
    {
        services.AddHostedService<IdentitySeeder>();

        return services;
    }
}
