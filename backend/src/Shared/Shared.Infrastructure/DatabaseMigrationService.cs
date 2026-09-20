using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Kernel.Abstractions;

namespace Shared.Infrastructure;

// Applies every module's migrations at start-up when Database:MigrateOnStartup is true.
// Registered before the module seeders, so they always find their tables.
public sealed class DatabaseMigrationService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<DatabaseMigrationService> logger
) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        if (!configuration.GetValue<bool>("Database:MigrateOnStartup"))
            return;

        using var scope = scopeFactory.CreateScope();

        foreach (var migrator in scope.ServiceProvider.GetServices<IModuleMigrator>())
        {
            await migrator.MigrateAsync(ct);
            logger.LogInformation("Database migrated: {Module}", migrator.ModuleName);
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
