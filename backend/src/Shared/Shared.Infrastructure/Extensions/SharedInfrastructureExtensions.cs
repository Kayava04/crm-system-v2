using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared.Kernel.Abstractions;

namespace Shared.Infrastructure.Extensions;

public static class SharedInfrastructureExtensions
{
    public static IServiceCollection AddSharedInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' not found in configuration.");

        services.AddScoped(_ => new SharedDbConnection(connectionString));
        services.AddScoped<TransactionCoordinator>();
        services.AddScoped<ITransactionCoordinator>(sp => sp.GetRequiredService<TransactionCoordinator>());
        services.AddSingleton<TransactionEnlistmentInterceptor>();
        services.AddSingleton<IAdvisoryLock>(_ => new PostgresAdvisoryLock(connectionString));

        services.AddSingleton<IFileStorage>(sp =>
        {
            var configured = sp.GetRequiredService<IConfiguration>()["Storage:Path"];
            var path = string.IsNullOrWhiteSpace(configured) ? "uploads" : configured;

            return new LocalFileStorage(Path.IsPathRooted(path)
                ? path
                : Path.Combine(sp.GetRequiredService<IHostEnvironment>().ContentRootPath, path));
        });

        services.AddHostedService<DatabaseMigrationService>();

        return services;
    }

    public static IServiceCollection AddModuleDbContext<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        services.AddDbContext<TContext>((sp, options) => options
            .UseNpgsql(sp.GetRequiredService<SharedDbConnection>().Connection)
            .AddInterceptors(sp.GetRequiredService<TransactionEnlistmentInterceptor>()));

        services.AddScoped<IModuleMigrator, ModuleMigrator<TContext>>();

        return services;
    }
}
