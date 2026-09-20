namespace Shared.Kernel.Abstractions;

// One per module database; lets the host apply all module migrations without knowing the DbContext types
public interface IModuleMigrator
{
    string ModuleName { get; }

    Task MigrateAsync(CancellationToken ct = default);
}
