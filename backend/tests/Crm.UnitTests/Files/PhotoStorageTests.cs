using System.Text;
using Identity.Application.Services;
using Shared.Infrastructure;

namespace Crm.UnitTests.Files;

public class PhotoStorageTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "crm-unit-storage-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    // ------------------------------------------------------------------ what is a picture
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46];
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00];
    private static readonly byte[] Webp = [.. "RIFF"u8, 0, 0, 0, 0, .. "WEBP"u8, .. "VP8 "u8];
    private static readonly byte[] Wav = [.. "RIFF"u8, 0, 0, 0, 0, .. "WAVE"u8, .. "fmt "u8];

    [Fact]
    public void JPEG_PNG_and_WebP_are_recognised_by_their_first_bytes()
    {
        var jpeg = ImageFormat.Detect(Jpeg)!;
        var png = ImageFormat.Detect(Png)!;
        var webp = ImageFormat.Detect(Webp)!;

        Assert.Equal(("image/jpeg", ".jpg"), (jpeg.ContentType, jpeg.Extension));
        Assert.Equal(("image/png", ".png"), (png.ContentType, png.Extension));
        Assert.Equal(("image/webp", ".webp"), (webp.ContentType, webp.Extension));
    }

    [Theory]
    [InlineData("GIF89a......")]
    [InlineData("<svg xmlns='http://www.w3.org/2000/svg'><script>alert(1)</script></svg>")]
    [InlineData("<?xml version='1.0'?><svg/>")]
    [InlineData("MZ this is how a Windows executable starts")]
    [InlineData("#!/bin/sh")]
    [InlineData("<html><script>alert(1)</script></html>")]
    [InlineData("just text")]
    [InlineData("")]
    public void Everything_else_is_refused(string content)
    {
        Assert.Null(ImageFormat.Detect(Encoding.Latin1.GetBytes(content)));
    }

    [Fact]
    public void A_truncated_header_or_a_riff_file_that_is_not_webp_is_refused()
    {
        Assert.Null(ImageFormat.Detect([0xFF, 0xD8]));
        Assert.Null(ImageFormat.Detect([0x89, 0x50, 0x4E, 0x47]));
        Assert.Null(ImageFormat.Detect(Wav));   // a WAV sound also starts with RIFF
    }

    // ------------------------------------------------------------------ the storage folder
    [Fact]
    public async Task A_saved_file_gets_a_generated_name_inside_its_folder_and_can_be_read_and_deleted()
    {
        var storage = new LocalFileStorage(_root);

        var key = await storage.SaveAsync("photos/abc", ".jpg", new MemoryStream(Jpeg));

        Assert.Matches(@"^photos/abc/[0-9a-f]{32}\.jpg$", key);
        Assert.True(File.Exists(Path.Combine(_root, "photos", "abc", Path.GetFileName(key))));

        await using (var stream = await storage.OpenReadAsync(key))
        {
            var read = new MemoryStream();
            await stream!.CopyToAsync(read);
            Assert.Equal(Jpeg, read.ToArray());
        }

        await storage.DeleteAsync(key);
        Assert.Null(await storage.OpenReadAsync(key));
        await storage.DeleteAsync(key);   // deleting twice is fine
    }

    [Fact]
    public async Task Two_uploads_never_get_the_same_name()
    {
        var storage = new LocalFileStorage(_root);

        var keys = new HashSet<string>();
        for (var i = 0; i < 50; i++)
            keys.Add(await storage.SaveAsync("photos", ".png", new MemoryStream(Png)));

        Assert.Equal(50, keys.Count);
    }

    [Theory]
    [InlineData("../evil")]
    [InlineData("photos/../../evil")]
    [InlineData("/etc")]
    [InlineData("photos//x")]
    [InlineData("photos/x y")]
    [InlineData("")]
    [InlineData("photos\\x")]
    public async Task A_folder_that_could_leave_the_storage_is_refused(string folder)
    {
        var storage = new LocalFileStorage(_root);

        await Assert.ThrowsAsync<ArgumentException>(() => storage.SaveAsync(folder, ".jpg", new MemoryStream(Jpeg)));
    }

    [Theory]
    [InlineData("jpg")]
    [InlineData("../x.jpg")]
    [InlineData(".jpg/../../x")]
    [InlineData(".")]
    [InlineData("")]
    public async Task An_extension_that_is_not_a_plain_extension_is_refused(string extension)
    {
        var storage = new LocalFileStorage(_root);

        await Assert.ThrowsAsync<ArgumentException>(() => storage.SaveAsync("photos", extension, new MemoryStream(Jpeg)));
    }

    [Theory]
    [InlineData("../secret.txt")]
    [InlineData("photos/../../secret.txt")]
    [InlineData("/etc/passwd")]
    [InlineData("")]
    public async Task Reading_or_deleting_outside_the_storage_is_refused(string key)
    {
        var storage = new LocalFileStorage(_root);

        await Assert.ThrowsAsync<ArgumentException>(() => storage.OpenReadAsync(key));
        await Assert.ThrowsAsync<ArgumentException>(() => storage.DeleteAsync(key));
    }
}
