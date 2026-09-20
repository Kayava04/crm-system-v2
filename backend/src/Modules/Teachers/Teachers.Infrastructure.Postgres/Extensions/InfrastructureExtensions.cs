using Shared.Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Teachers.Application.Abstractions;
using Teachers.Infrastructure.Postgres.Persistence;
using Teachers.Infrastructure.Postgres.Repositories;

namespace Teachers.Infrastructure.Postgres.Extensions;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddTeachersInfrastructure(
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
services.AddModuleDbContext<TeachersDbContext>();

        services.AddScoped<ITeacherUnitOfWork>(sp => sp.GetRequiredService<TeachersDbContext>());

        return services;
    }

    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<ITeacherRepository, TeacherRepository>();

        return services;
    }
}
