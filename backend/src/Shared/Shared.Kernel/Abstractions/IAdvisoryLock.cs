namespace Shared.Kernel.Abstractions;

public interface IAdvisoryLock
{
    Task<IAsyncDisposable?> TryAcquireAsync(string name, CancellationToken ct = default);
}
