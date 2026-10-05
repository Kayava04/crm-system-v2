namespace Shared.Kernel.Abstractions;

public interface IFileStorage
{
    Task<string> SaveAsync(string folder, string extension, Stream content, CancellationToken ct = default);

    Task<Stream?> OpenReadAsync(string key, CancellationToken ct = default);

    Task DeleteAsync(string key, CancellationToken ct = default);
}
