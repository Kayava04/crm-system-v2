using FluentValidation;
using Materials.Application.Features.CreateMaterial;
using Materials.Application.Features.DeleteMaterial;
using Materials.Application.Features.GetMaterialById;
using Materials.Application.Features.GetMaterials;
using Materials.Application.Features.UpdateMaterial;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Materials.Application.Extensions;

public static class ApplicationExtensions
{
    public static IServiceCollection AddMaterialsApplication(this IServiceCollection services)
    {
        // Register FluentValidation
        services.AddValidatorsFromAssembly(typeof(ApplicationExtensions).Assembly);

        return services;
    }

    public static IEndpointRouteBuilder MapMaterialsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/materials")
                       .WithTags("Materials");

        GetAllEndpoint.Map(group);
        GetByIdEndpoint.Map(group);
        CreateEndpoint.Map(group);
        UpdateEndpoint.Map(group);
        DeleteEndpoint.Map(group);

        return app;
    }
}
