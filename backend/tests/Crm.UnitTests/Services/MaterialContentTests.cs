using Materials.Application.Services;
using Materials.Domain.Enums;

namespace Crm.UnitTests.Services;

public class MaterialContentTests
{
    [Fact]
    public void Video_keeps_only_the_video_id()
    {
        var (body, url, videoId) = MaterialContent.Resolve(MaterialType.Video, "ignored", "https://youtu.be/dQw4w9WgXcQ");

        Assert.Null(body);
        Assert.Null(url);
        Assert.Equal("dQw4w9WgXcQ", videoId);
    }

    [Fact]
    public void Article_keeps_only_the_trimmed_text()
    {
        var (body, url, videoId) = MaterialContent.Resolve(MaterialType.Article, "  # Title  ", "https://ignored.example");

        Assert.Equal("# Title", body);
        Assert.Null(url);
        Assert.Null(videoId);
    }

    [Fact]
    public void Link_keeps_only_the_trimmed_url()
    {
        var (body, url, videoId) = MaterialContent.Resolve(MaterialType.Link, "ignored", "  https://example.com/page ");

        Assert.Null(body);
        Assert.Equal("https://example.com/page", url);
        Assert.Null(videoId);
    }

    [Theory]
    [InlineData("https://example.com", true)]
    [InlineData("http://example.com/a?b=c", true)]
    [InlineData("javascript:alert(1)", false)]
    [InlineData("ftp://example.com", false)]
    [InlineData("example.com", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Validates_http_urls(string? url, bool expected)
    {
        Assert.Equal(expected, MaterialContent.IsValidHttpUrl(url));
    }
}
