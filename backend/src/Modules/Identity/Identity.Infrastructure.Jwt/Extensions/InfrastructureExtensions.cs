using Identity.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.Infrastructure.Jwt.Extensions;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddJwtInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.Configure<JwtOptions>(
            configuration.GetSection(JwtOptions.SectionName));

        services.AddScoped<ITokenService, TokenService>();

        return services;
    }
}
