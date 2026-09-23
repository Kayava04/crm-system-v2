using Shared.Kernel.Primitives;

namespace Identity.Domain.Entities;

// The profile photo of an account: the image itself is a file in the storage, this row says which file and what it is
public sealed class UserPhoto : Entity
{
    public Guid UserId { get; private set; }
    public string StorageKey { get; private set; } = string.Empty;
    public string OriginalFileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }
    public DateTime UploadedAt { get; private set; }

    private UserPhoto() { }

    public static UserPhoto Create(Guid userId, string storageKey, string originalFileName, string contentType, long sizeBytes)
    {
        return new UserPhoto
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            StorageKey = storageKey,
            OriginalFileName = originalFileName,
            ContentType = contentType,
            SizeBytes = sizeBytes,
            UploadedAt = DateTime.UtcNow
        };
    }

    public void Replace(string storageKey, string originalFileName, string contentType, long sizeBytes)
    {
        StorageKey = storageKey;
        OriginalFileName = originalFileName;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        UploadedAt = DateTime.UtcNow;
    }
}
