using Billing.Application.Extensions;
using Courses.Application.Extensions;
using Enrollments.Application.Extensions;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Identity.Application.Extensions;
using Materials.Application.Extensions;
using Notifications.Application.Extensions;
using Reporting.Application.Extensions;
using Scalar.AspNetCore;
using Scheduling.Application.Extensions;
using Students.Application.Extensions;
using Teachers.Application.Extensions;

namespace Host.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication Configure(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseHttpsRedirection();
        app.UseCors(ServiceCollectionExtensions.CorsPolicyName);
        app.UseAuthentication();
        app.UseAuthorization();

        app
            .MapEndpoints()
            .MapHealthEndpoints()
            .MapOpenApi();

        app.MapScalarApiReference();

        return app;
    }

    private static WebApplication MapEndpoints(this WebApplication app)
    {
        app.MapIdentityEndpoints();
        app.MapStudentsEndpoints();
        app.MapTeachersEndpoints();
        app.MapCoursesEndpoints();
        app.MapEnrollmentsEndpoints();
        app.MapSchedulingEndpoints();
        app.MapBillingEndpoints();
        app.MapReportingEndpoints();
        app.MapNotificationsEndpoints();
        app.MapMaterialsEndpoints();

        return app;
    }

    // /health: the process is alive (no dependencies checked). /health/ready: the database is usable.
    private static WebApplication MapHealthEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = WriteResponse
        }).AllowAnonymous();

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = WriteResponse
        }).AllowAnonymous();

        return app;
    }

    private static Task WriteResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        return context.Response.WriteAsJsonAsync(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                durationMs = (int)e.Value.Duration.TotalMilliseconds
            })
        });
    }
}
