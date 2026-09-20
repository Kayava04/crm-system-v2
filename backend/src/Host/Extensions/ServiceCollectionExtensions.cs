using System.Text;
using System.Text.Json.Serialization;
using Identity.Application.Extensions;
using Identity.Contracts.Enums;
using Identity.Infrastructure.Jwt;
using Identity.Infrastructure.Jwt.Extensions;
using Identity.Infrastructure.Postgres.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Students.Application.Extensions;
using Students.Infrastructure.Postgres.Extensions;
using Teachers.Application.Extensions;
using Teachers.Infrastructure.Postgres.Extensions;
using Courses.Application.Extensions;
using Courses.Infrastructure.Postgres.Extensions;
using Enrollments.Application.Extensions;
using Enrollments.Infrastructure.Postgres.Extensions;
using Scheduling.Application.Extensions;
using Billing.Application.Extensions;
using Billing.Infrastructure.Postgres.Extensions;
using Scheduling.Infrastructure.Postgres.Extensions;

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
            .AddAuthentication(configuration)
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
            .AddStudentsInfrastructure(configuration)
            .AddIdentityApplication()
            .AddIdentityInfrastructure(configuration)
            .AddJwtInfrastructure(configuration)
            .AddTeachersApplication()
            .AddTeachersInfrastructure(configuration)
            .AddCoursesApplication()
            .AddCoursesInfrastructure(configuration)
            .AddEnrollmentsApplication()
            .AddEnrollmentsInfrastructure(configuration)
            .AddSchedulingApplication()
            .AddSchedulingInfrastructure(configuration)
            .AddBillingApplication()
            .AddBillingInfrastructure(configuration);

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

            options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
        });

        return services;
    }

    private static IServiceCollection AddAuthentication(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var jwtSection = configuration.GetSection(JwtOptions.SectionName);
        var secretKey = jwtSection["SecretKey"]
            ?? throw new InvalidOperationException("Jwt:SecretKey not found in configuration.");

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSection["Issuer"],
                    ValidAudience = jwtSection["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
                };
            });

        var authorizationBuilder = services.AddAuthorizationBuilder();

        foreach (var permission in Enum.GetValues<SystemPermission>())
        {
            var permissionName = permission.ToString();

            authorizationBuilder.AddPolicy(permissionName, policy =>
                policy.RequireClaim("permission", permissionName));
        }

        return services;
    }
}
