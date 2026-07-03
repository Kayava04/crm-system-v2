using Identity.Application.Extensions;
using Scalar.AspNetCore;
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
        app.MapStudentsEndpoints();
        app.MapIdentityEndpoints();
        app.MapTeachersEndpoints();

        return app;
    }
}
