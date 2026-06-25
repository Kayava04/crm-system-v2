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
            .AddApiDocumentation();

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

    private static IServiceCollection AddJsonOptions(this IServiceCollection services)
    {
        services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        return services;
    }

    private static IServiceCollection AddApiDocumentation(this IServiceCollection services)
    {
        services.AddOpenApi("v1", options =>
        {
            options.AddDocumentTransformer((document, context, ct) =>
            {
                document.Info = new()
                {
                    Title = "CRM System API",
                    Version = "v1",
                    Description = "REST API for managing a foreign language school"
                };

                return Task.CompletedTask;
            });
        });

        return services;
    }
}
