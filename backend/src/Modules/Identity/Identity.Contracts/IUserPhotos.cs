namespace Identity.Contracts;

// Lets other modules show the photo of an account they know (a student's or a teacher's user id)
public interface IUserPhotos
{
    // null when the user has no photo. The caller owns the stream and must dispose it.
    Task<UserPhotoFile?> OpenAsync(Guid userId, CancellationToken ct = default);
}

public sealed record UserPhotoFile(Stream Content, string ContentType, Guid Version, DateTime UploadedAt);
