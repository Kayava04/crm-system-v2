using Enrollments.Application.Abstractions;
using Enrollments.Infrastructure.Postgres.Persistence;
using Enrollments.Infrastructure.Postgres.Repositories;
using Enrollments.Infrastructure.Postgres.Services;
using Shared.Infrastructure.Extensions;
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
services.AddModuleDbContext<EnrollmentsDbContext>();

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
