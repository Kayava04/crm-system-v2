using Materials.Domain.Enums;

namespace Materials.Application.Services;

// Keeps only the fields relevant for the material type so nothing stale is stored
internal static class MaterialContent
{
    public static (string? Body, string? Url, string? YouTubeVideoId) Resolve(
        MaterialType type,
        string? body,
        string? url) =>
        type switch
        {
            MaterialType.Video => (null, null, YouTubeUrl.TryGetVideoId(url)),
            MaterialType.Article => (body?.Trim(), null, null),
            _ => (null, url?.Trim(), null)
        };

    public static bool IsValidHttpUrl(string? url) =>
        Uri.TryCreate(url?.Trim(), UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https";
}
