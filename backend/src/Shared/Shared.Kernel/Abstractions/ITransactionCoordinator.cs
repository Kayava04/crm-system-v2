namespace Shared.Kernel.Abstractions;

public interface ITransactionCoordinator
{
    Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct = default);

    Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken ct = default);

    Task AcquireLockAsync(string name, CancellationToken ct = default);
}
