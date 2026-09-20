using System.Text.RegularExpressions;
using Shared.Kernel.Abstractions;

namespace Shared.Infrastructure;

// Files in a folder of the server (Storage:Path). Every key is checked so it cannot leave that folder.
public sealed partial class LocalFileStorage : IFileStorage
{
    private readonly string _root;

    public LocalFileStorage(string rootPath)
    {
        _root = Path.GetFullPath(rootPath);
        Directory.CreateDirectory(_root);
    }

    public string Root => _root;

    [GeneratedRegex(@"^[A-Za-z0-9_\-]+(/[A-Za-z0-9_\-]+)*$")]
    private static partial Regex SafeFolder();

    [GeneratedRegex(@"^\.[A-Za-z0-9]{1,10}$")]
    private static partial Regex SafeExtension();

    public async Task<string> SaveAsync(string folder, string extension, Stream content, CancellationToken ct = default)
    {
        if (!SafeFolder().IsMatch(folder))
            throw new ArgumentException("The folder may contain only letters, digits, '-', '_' and '/'.", nameof(folder));

        if (!SafeExtension().IsMatch(extension))
            throw new ArgumentException("The extension must look like '.jpg'.", nameof(extension));

        var key = $"{folder}/{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var path = Resolve(key);

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        await using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await content.CopyToAsync(file, ct);

        return key;
    }

    public Task<Stream?> OpenReadAsync(string key, CancellationToken ct = default)
    {
        var path = Resolve(key);

        return Task.FromResult<Stream?>(File.Exists(path)
            ? new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true)
            : null);
    }

    public Task DeleteAsync(string key, CancellationToken ct = default)
    {
        var path = Resolve(key);

        if (File.Exists(path))
            File.Delete(path);

        return Task.CompletedTask;
    }

    // A key comes from our own database, but it is checked anyway: nothing may resolve outside the storage folder
    internal string Resolve(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || Path.IsPathRooted(key) || key.Contains(".."))
            throw new ArgumentException("Invalid storage key.", nameof(key));

        var full = Path.GetFullPath(Path.Combine(_root, key));

        if (!full.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new ArgumentException("Invalid storage key.", nameof(key));

        return full;
    }
}
