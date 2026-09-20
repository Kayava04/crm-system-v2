using System.Text;
using System.Threading.RateLimiting;
using System.Text.Json.Serialization;
using Identity.Application.Extensions;
using Identity.Contracts.Enums;
using Identity.Infrastructure.Jwt;
using Identity.Infrastructure.Jwt.Extensions;
using Identity.Infrastructure.Postgres.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Shared.Infrastructure.Extensions;
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
using Materials.Application.Extensions;
using Materials.Infrastructure.Postgres.Extensions;
using Notifications.Application.Extensions;
using Notifications.Infrastructure.Postgres.Extensions;
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
            .AddCorsPolicy(configuration)
            .AddAuthRateLimiting(configuration)
            .AddHealth()
            .AddApiDocumentation();

        services.AddExceptionHandler<UniqueViolationExceptionHandler>();
        services.AddProblemDetails();

        return services;
    }

    private static IServiceCollection AddModules(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services
            .AddSharedInfrastructure(configuration)
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
            .AddBillingInfrastructure(configuration)
            .AddNotificationsApplication()
            .AddNotificationsInfrastructure(configuration)
            .AddMaterialsApplication()
            .AddMaterialsInfrastructure(configuration);

        return services;
    }

    public const string CorsPolicyName = "Frontend";

    // Origins of the frontend (e.g. the React dev server) come from Cors:AllowedOrigins; with none configured
    // no cross-origin request is allowed. Tokens travel in the Authorization header, so no credentials are needed.
    private static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration configuration)
    {
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        services.AddCors(options => options.AddPolicy(CorsPolicyName, policy =>
        {
            if (origins.Length == 0)
                return;

            policy.WithOrigins(origins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .WithExposedHeaders("Location")
                  .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
        }));

        return services;
    }

    // Login and token refresh are limited per client address, so a password cannot be guessed at machine speed.
    // RateLimiting:Auth:PermitLimit requests per WindowSeconds (default 30 per minute).
    private static IServiceCollection AddAuthRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var permitLimit = Math.Max(1, configuration.GetValue("RateLimiting:Auth:PermitLimit", 30));
        var window = TimeSpan.FromSeconds(Math.Max(1, configuration.GetValue("RateLimiting:Auth:WindowSeconds", 60)));

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy("auth", httpContext => RateLimitPartition.GetFixedWindowLimiter(
                httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = permitLimit, Window = window, QueueLimit = 0 }));

            options.OnRejected = async (context, token) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    context.HttpContext.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();

                context.HttpContext.Response.ContentType = "application/problem+json";
                await context.HttpContext.Response.WriteAsJsonAsync(new Microsoft.AspNetCore.Mvc.ProblemDetails
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Title = "Too many requests",
                    Detail = "Too many attempts. Wait a little and try again."
                }, token);
            };
        });

        return services;
    }

    private static IServiceCollection AddHealth(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);

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

        // Fail at start-up with a clear message instead of on the first login
        if (secretKey.Length < 32)
            throw new InvalidOperationException(
                "Jwt:SecretKey must be at least 32 characters long. Set it with: dotnet user-secrets set \"Jwt:SecretKey\" \"<random text>\".");

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
