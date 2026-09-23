using System.Text.RegularExpressions;

namespace Materials.Application.Services;

internal static partial class YouTubeUrl
{
    private static readonly HashSet<string> Hosts =
    [
        "youtube.com", "www.youtube.com", "m.youtube.com", "music.youtube.com",
        "youtube-nocookie.com", "www.youtube-nocookie.com", "youtu.be"
    ];

    [GeneratedRegex("^[A-Za-z0-9_-]{11}$")]
    private static partial Regex VideoIdPattern();

    // Supports watch?v=, youtu.be/, /embed/, /shorts/ and /live/ links
    public static string? TryGetVideoId(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)
            || !Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)
            || uri.Scheme is not ("http" or "https")
            || !Hosts.Contains(uri.Host.ToLowerInvariant()))
            return null;

        string? candidate;

        if (uri.Host.Equals("youtu.be", StringComparison.OrdinalIgnoreCase))
        {
            candidate = uri.AbsolutePath.Trim('/');
        }
        else
        {
            var segments = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);

            candidate = segments.Length switch
            {
                >= 2 when segments[0] is "embed" or "shorts" or "live" => segments[1],
                1 when segments[0] == "watch" => GetQueryValue(uri.Query, "v"),
                _ => null
            };
        }

        return candidate is not null && VideoIdPattern().IsMatch(candidate) ? candidate : null;
    }

    public static string EmbedUrl(string videoId) => $"https://www.youtube-nocookie.com/embed/{videoId}";

    public static string ThumbnailUrl(string videoId) => $"https://img.youtube.com/vi/{videoId}/hqdefault.jpg";

    private static string? GetQueryValue(string query, string key)
    {
        foreach (var pair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);

            if (parts.Length == 2 && parts[0] == key)
                return Uri.UnescapeDataString(parts[1]);
        }

        return null;
    }
}
