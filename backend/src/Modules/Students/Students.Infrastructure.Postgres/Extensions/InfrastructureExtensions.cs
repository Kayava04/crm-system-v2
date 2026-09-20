using Shared.Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Students.Application.Abstractions;
using Students.Infrastructure.Postgres.Persistence;
using Students.Infrastructure.Postgres.Repositories;

namespace Students.Infrastructure.Postgres.Extensions;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddStudentsInfrastructure(
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
            ?? throw new InvalidOperationException($"Connection string 'Default' not found in configuration.");

        services.AddModuleDbContext<StudentsDbContext>();

        services.AddScoped<IStudentUnitOfWork>(sp => sp.GetRequiredService<StudentsDbContext>());

        return services;
    }

    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IStudentRepository, StudentRepository>();

        return services;
    }
}
