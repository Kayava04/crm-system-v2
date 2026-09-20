namespace Shared.Kernel.Abstractions;

// A named lock shared by every instance of the application (backed by the database), so a periodic job
// runs on one instance at a time. TryAcquire returns null when another holder has it; disposing the handle releases it.
public interface IAdvisoryLock
{
    Task<IAsyncDisposable?> TryAcquireAsync(string name, CancellationToken ct = default);
}
