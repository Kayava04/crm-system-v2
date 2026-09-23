using Microsoft.EntityFrameworkCore;
using Shared.Kernel.Abstractions;

namespace Shared.Infrastructure;

public sealed class ModuleMigrator<TContext>(TContext context) : IModuleMigrator
    where TContext : DbContext
{
    public string ModuleName => typeof(TContext).Name.Replace("DbContext", string.Empty);

    public async Task MigrateAsync(CancellationToken ct = default) =>
        await context.Database.MigrateAsync(ct);
}
