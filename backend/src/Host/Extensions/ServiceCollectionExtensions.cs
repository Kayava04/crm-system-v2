using System.Text.Json.Serialization;
using Students.Application.Extensions;
using Students.Infrastructure.Postgres.Extensions;

namespace Host.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services
            .AddModules(configuration)
            .AddJsonOptions()
            .AddOpenApi();

        return services;
    }

    private static IServiceCollection AddModules(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services
            .AddStudentsApplication()
            .AddStudentsInfrastructure(configuration);

        return services;
    }

    private static IServiceCollection AddJsonOptions(
    this IServiceCollection services)
    {
        services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        return services;
    }
}
