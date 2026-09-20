using Billing.Application.Extensions;
using Courses.Application.Extensions;
using Enrollments.Application.Extensions;
using Identity.Application.Extensions;
using Scalar.AspNetCore;
using Scheduling.Application.Extensions;
using Students.Application.Extensions;
using Teachers.Application.Extensions;

namespace Host.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication Configure(this WebApplication app)
    {
        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();

        app
            .MapEndpoints()
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

        return app;
    }
}
