using Enrollments.Application.Abstractions;
using Enrollments.Infrastructure.Postgres.Persistence;
using Enrollments.Infrastructure.Postgres.Repositories;
using Enrollments.Infrastructure.Postgres.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Enrollments.Infrastructure.Postgres.Extensions;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddEnrollmentsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services
            .AddDatabase(configuration)
            .AddRepositories()
            .AddServices();

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

        services.AddDbContext<EnrollmentsDbContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<IEnrollmentUnitOfWork>(sp => sp.GetRequiredService<EnrollmentsDbContext>());

        return services;
    }

    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();

        return services;
    }

    private static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddScoped<IEnrollmentNumberGenerator, EnrollmentNumberGenerator>();

        return services;
    }
}
