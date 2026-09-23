using Materials.Application.Services;

namespace Crm.UnitTests.Services;

public class YouTubeUrlTests
{
    [Theory]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ&t=10s")]
    [InlineData("https://youtube.com/watch?feature=share&v=dQw4w9WgXcQ")]
    [InlineData("https://m.youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://music.youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ?si=abc")]
    [InlineData("https://www.youtube.com/embed/dQw4w9WgXcQ")]
    [InlineData("https://www.youtube-nocookie.com/embed/dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/shorts/dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/live/dQw4w9WgXcQ")]
    [InlineData("  https://youtu.be/dQw4w9WgXcQ  ")]
    public void Extracts_the_video_id(string url)
    {
        Assert.Equal("dQw4w9WgXcQ", YouTubeUrl.TryGetVideoId(url));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not a url")]
    [InlineData("javascript:alert(1)")]
    [InlineData("ftp://youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://evil.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://youtube.com.evil.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://notyoutube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/watch?v=short")]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ_toolong")]
    [InlineData("https://www.youtube.com/watch")]
    [InlineData("https://www.youtube.com/")]
    [InlineData("https://www.youtube.com/embed/")]
    [InlineData("https://youtu.be/")]
    public void Rejects_everything_else(string? url)
    {
        Assert.Null(YouTubeUrl.TryGetVideoId(url));
    }

    [Fact]
    public void Builds_the_privacy_friendly_embed_and_thumbnail_urls()
    {
        Assert.Equal("https://www.youtube-nocookie.com/embed/dQw4w9WgXcQ", YouTubeUrl.EmbedUrl("dQw4w9WgXcQ"));
        Assert.Equal("https://img.youtube.com/vi/dQw4w9WgXcQ/hqdefault.jpg", YouTubeUrl.ThumbnailUrl("dQw4w9WgXcQ"));
    }
}
