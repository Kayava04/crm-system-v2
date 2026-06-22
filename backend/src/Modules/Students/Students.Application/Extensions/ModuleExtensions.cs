using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Students.Infrastructure.Postgres.Extensions;

namespace Students.Application.Extensions;

public static class ModuleExtensions
{
    public static IServiceCollection AddStudentsModule(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        // FluentValidation
        services.AddValidatorsFromAssembly(typeof(ModuleExtensions).Assembly);

        // Postgres Infrastructure
        services.AddInfrastructure(configuration);

        return services;
    }

    public static IEndpointRouteBuilder MapStudentsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/students")
                       .WithTags("Students");

        return app;
    }
}
