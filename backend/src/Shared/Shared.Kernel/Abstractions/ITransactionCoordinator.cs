namespace Shared.Kernel.Abstractions;

// Runs several modules' changes as one database transaction: either everything is saved or nothing.
// Each module keeps its own DbContext and schema; inside the action their SaveChanges calls share one transaction.
public interface ITransactionCoordinator
{
    Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct = default);

    Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken ct = default);

    // Waits until nobody else holds the named lock, then holds it until the surrounding transaction ends.
    // Use it around "check, then write" sequences so two requests at the same moment cannot both pass the check.
    // Only valid inside ExecuteAsync. When taking several locks, take them in a fixed (sorted) order.
    Task AcquireLockAsync(string name, CancellationToken ct = default);
}
