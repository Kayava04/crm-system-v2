namespace Shared.Kernel.Abstractions;

public interface IModuleMigrator
{
    string ModuleName { get; }

    Task MigrateAsync(CancellationToken ct = default);
}
