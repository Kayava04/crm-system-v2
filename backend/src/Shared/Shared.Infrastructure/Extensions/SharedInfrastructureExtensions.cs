using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Kernel.Abstractions;

namespace Shared.Infrastructure.Extensions;

public static class SharedInfrastructureExtensions
{
    // Must be registered before the modules: it owns the connection and the migrations that run before the seeders
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

        services.AddHostedService<DatabaseMigrationService>();

        return services;
    }

    // A module database: same physical connection for every module, so a shared transaction is possible
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
