using Microsoft.EntityFrameworkCore;
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
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "Connection string 'Default' not found in configuration.");

        services.AddDbContext<TeachersDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<ITeacherUnitOfWork>(sp => sp.GetRequiredService<TeachersDbContext>());

        return services;
    }

    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<ITeacherRepository, TeacherRepository>();

        return services;
    }
}
