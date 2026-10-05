namespace Identity.Contracts;

public interface IUserPhotos
{
    Task<UserPhotoFile?> OpenAsync(Guid userId, CancellationToken ct = default);
}

public sealed record UserPhotoFile(Stream Content, string ContentType, Guid Version, DateTime UploadedAt);
