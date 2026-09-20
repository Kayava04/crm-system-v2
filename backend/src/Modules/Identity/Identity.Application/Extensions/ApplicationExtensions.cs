using FluentValidation;
using Identity.Contracts;
using Identity.Application.Features.ChangePassword;
using Identity.Application.Features.GetPermissions;
using Identity.Application.Features.GetRoles;
using Identity.Application.Features.DeleteMyPhoto;
using Identity.Application.Features.GetMyPhoto;
using Identity.Application.Features.Login;
using Identity.Application.Features.Me;
using Identity.Application.Features.Refresh;
using Identity.Application.Features.Register;
using Identity.Application.Features.UpdateMyContact;
using Identity.Application.Features.UploadMyPhoto;
using Identity.Application.Services;
using Identity.Application.Features.ResetPassword;
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

        services.AddScoped<IUserAccountManager, UserAccountManager>();
        services.AddScoped<UserPhotoService>();
        services.AddScoped<IUserPhotos>(sp => sp.GetRequiredService<UserPhotoService>());
        services.AddScoped<IUserDirectory, UserDirectoryService>();

        return services;
    }

    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
                       .WithTags("Identity");

        LoginEndpoint.Map(group);
        RegisterEndpoint.Map(group);
        ChangePasswordEndpoint.Map(group);
        MeEndpoint.Map(group);
        UpdateMyContactEndpoint.Map(group);
        UploadMyPhotoEndpoint.Map(group);
        GetMyPhotoEndpoint.Map(group);
        DeleteMyPhotoEndpoint.Map(group);
        ResetPasswordEndpoint.Map(group);
        RefreshEndpoint.Map(group);

        GetRolesEndpoint.Map(group);
        GetPermissionsEndpoint.Map(group);

        return app;
    }
}
