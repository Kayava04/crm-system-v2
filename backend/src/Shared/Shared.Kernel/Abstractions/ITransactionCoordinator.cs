namespace Shared.Kernel.Abstractions;

// Runs several modules' changes as one database transaction: either everything is saved or nothing.
// Each module keeps its own DbContext and schema; inside the action their SaveChanges calls share one transaction.
public interface ITransactionCoordinator
{
    Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct = default);

    Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken ct = default);
}
