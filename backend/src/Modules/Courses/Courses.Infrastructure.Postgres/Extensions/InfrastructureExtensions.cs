using Courses.Application.Abstractions;
using Courses.Infrastructure.Postgres.Persistence;
using Courses.Infrastructure.Postgres.Repositories;
using Shared.Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Courses.Infrastructure.Postgres.Extensions;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddCoursesInfrastructure(
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
services.AddModuleDbContext<CoursesDbContext>();

        services.AddScoped<ICoursesUnitOfWork>(sp => sp.GetRequiredService<CoursesDbContext>());

        return services;
    }

    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<ICourseRepository, CourseRepository>();

        return services;
    }
}
