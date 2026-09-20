namespace Identity.Application.Services;

// What a file really is is decided by its first bytes, not by the name or the type the client claims.
// Only formats a browser shows as a plain picture are accepted (no SVG, which can carry scripts).
internal static class ImageFormat
{
    public sealed record Detected(string ContentType, string Extension);

    public static Detected? Detect(ReadOnlySpan<byte> header)
    {
        if (header.Length >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
            return new Detected("image/jpeg", ".jpg");

        if (header.Length >= 8 && header[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
            return new Detected("image/png", ".png");

        // "RIFF" + 4 size bytes + "WEBP"
        if (header.Length >= 12
            && header[..4].SequenceEqual("RIFF"u8)
            && header.Slice(8, 4).SequenceEqual("WEBP"u8))
            return new Detected("image/webp", ".webp");

        return null;
    }
}
