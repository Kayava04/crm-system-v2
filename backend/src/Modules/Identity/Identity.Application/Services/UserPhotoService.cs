using Identity.Application.Abstractions;
using Identity.Contracts;
using Identity.Domain.Entities;
using Microsoft.Extensions.Logging;
using Shared.Kernel.Abstractions;

namespace Identity.Application.Services;

public sealed record PhotoInfo(string ContentType, long SizeBytes, DateTime UploadedAt);

internal sealed class UserPhotoService(
    IUserPhotoRepository repository,
    IFileStorage storage,
    IIdentityUnitOfWork unitOfWork,
    ITransactionCoordinator transaction,
    ILogger<UserPhotoService> logger
) : IUserPhotos
{
    public const int MaxBytes = 5 * 1024 * 1024;
    private const string Folder = "photos";

    public async Task<UserPhotoFile?> OpenAsync(Guid userId, CancellationToken ct = default)
    {
        var photo = await repository.GetByUserIdAsync(userId, ct);
        if (photo is null)
            return null;

        var stream = await storage.OpenReadAsync(photo.StorageKey, ct);
        if (stream is null)
        {
            logger.LogError("Photo file {Key} of user {UserId} is missing from the storage", photo.StorageKey, userId);
            return null;
        }

        var version = Guid.TryParseExact(Path.GetFileNameWithoutExtension(photo.StorageKey), "N", out var stored) ? stored : photo.Id;

        return new UserPhotoFile(stream, photo.ContentType, version, photo.UploadedAt);
    }

    public async Task<PhotoInfo> SaveAsync(
        Guid userId,
        string originalFileName,
        ImageFormat.Detected format,
        byte[] content,
        CancellationToken ct = default)
    {
        await using var input = new MemoryStream(content);
        var key = await storage.SaveAsync($"{Folder}/{userId:N}", format.Extension, input, ct);

        var name = originalFileName.Length > 255 ? originalFileName[..255] : originalFileName;
        string? oldKey = null;

        try
        {
            await transaction.ExecuteAsync(async token =>
            {
                await transaction.AcquireLockAsync($"user-photo:{userId}", token);

                var existing = await repository.GetByUserIdAsync(userId, token);
                oldKey = existing?.StorageKey;

                if (existing is null)
                {
                    await repository.AddAsync(UserPhoto.Create(userId, key, name, format.ContentType, content.Length), token);
                }
                else
                {
                    existing.Replace(key, name, format.ContentType, content.Length);
                    await repository.UpdateAsync(existing, token);
                }

                await unitOfWork.SaveChangesAsync(token);
            }, ct);
        }
        catch
        {
            await storage.DeleteAsync(key, CancellationToken.None);
            throw;
        }

        if (oldKey is not null)
            await TryDeleteAsync(oldKey);

        logger.LogInformation("User {UserId} uploaded a photo ({Bytes} bytes)", userId, content.Length);

        return new PhotoInfo(format.ContentType, content.Length, DateTime.UtcNow);
    }

    public async Task<bool> DeleteAsync(Guid userId, CancellationToken ct = default)
    {
        var photo = await repository.GetByUserIdAsync(userId, ct);
        if (photo is null)
            return false;

        await repository.DeleteAsync(photo, ct);
        await unitOfWork.SaveChangesAsync(ct);
        await TryDeleteAsync(photo.StorageKey);

        logger.LogInformation("User {UserId} removed their photo", userId);

        return true;
    }

    private async Task TryDeleteAsync(string key)
    {
        try
        {
            await storage.DeleteAsync(key);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not delete the old photo file {Key}", key);
        }
    }
}
