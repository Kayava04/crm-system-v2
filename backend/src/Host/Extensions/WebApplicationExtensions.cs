using Scalar.AspNetCore;
using Students.Application.Extensions;

namespace Host.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication Configure(this WebApplication app)
    {
        app
            .MapEndpoints()
            .MapOpenApi();

        app.MapScalarApiReference();

        return app;
    }

    private static WebApplication MapEndpoints(this WebApplication app)
    {
        app.MapStudentsEndpoints();

        return app;
    }
}
