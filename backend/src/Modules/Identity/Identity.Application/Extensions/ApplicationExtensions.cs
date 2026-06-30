using FluentValidation;
using Identity.Application.Features.ChangePassword;
using Identity.Application.Features.Login;
using Identity.Application.Features.Refresh;
using Identity.Application.Features.Register;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.Application.Extensions;

public static class ApplicationExtensions
{
    public static IServiceCollection AddIdentityApplication(this IServiceCollection services)
    {
        // Register FluentValidation
        services.AddValidatorsFromAssembly(typeof(ApplicationExtensions).Assembly);

        return services;
    }

    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
                       .WithTags("Identity");

        LoginEndpoint.Map(group);
        RegisterEndpoint.Map(group);
        ChangePasswordEndpoint.Map(group);
        RefreshEndpoint.Map(group);

        return app;
    }
}
