using Students.Application.Extensions;

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
            .AddOpenApi();

        return services;
    }

    private static IServiceCollection AddModules(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services
            .AddStudentsModule(configuration);

        return services;
    }
}
