using System.Net;
using System.Net.Http.Headers;
using Identity.Application.Abstractions;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Crm.IntegrationTests.Tests;

// The profile photo of an account: the picture is a file in the storage folder, the database describes it
public class PhotoTests(CrmApiFactory factory) : ApiTest(factory)
{
    // What matters to the server is the start of the file; the rest is arbitrary payload
    private static byte[] Png(int size = 300, byte fill = 7) => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, .. Enumerable.Repeat(fill, size)];
    private static byte[] Jpeg(int size = 300, byte fill = 9) => [0xFF, 0xD8, 0xFF, 0xE0, .. Enumerable.Repeat(fill, size)];
    private static byte[] Webp(int size = 300) => [.. "RIFF"u8, 0, 0, 0, 0, .. "WEBP"u8, .. Enumerable.Repeat((byte)5, size)];

    private Task<ApiResponse> UploadAsync(string? token, byte[] content, string fileName = "me.png") =>
        Api.UploadAsync("/api/auth/me/photo", fileName, content, token, method: HttpMethod.Put);

    private string PhotosOf(Guid userId) => Path.Combine(Factory.UploadsPath, "photos", userId.ToString("N"));

    private int FilesOf(Guid userId) => Directory.Exists(PhotosOf(userId)) ? Directory.GetFiles(PhotosOf(userId)).Length : 0;

    // ------------------------------------------------------------------ every kind of user can have a photo
    [Fact]
    public async Task A_student_a_teacher_and_an_administrator_can_each_upload_and_see_their_own_photo()
    {
        var student = await Data.StudentUserAsync(await Data.StudentAsync());
        var teacher = await Data.TeacherUserAsync(await Data.TeacherAsync());
        var admin = await Data.UserAsync("Admin");          // no student or teacher profile at all
        var bare = await Data.UserAsync("Student");         // an account without a linked profile

        foreach (var (user, bytes) in new[] { (student, Png()), (teacher, Jpeg()), (admin, Webp()), (bare, Png(50)) })
        {
            (await UploadAsync(user.Token, bytes)).Expect(200);

            var served = await Api.DownloadAsync("/api/auth/me/photo", user.Token);
            served.Expect(200);
            Assert.Equal(bytes, served.Content);
        }
    }

    [Fact]
    public async Task The_super_admin_can_have_a_photo_too()
    {
        (await UploadAsync(Admin, Png())).Expect(200);
        Assert.Equal(Png(), (await Api.DownloadAsync("/api/auth/me/photo", Admin)).Expect(200).Content);

        (await Api.DeleteAsync("/api/auth/me/photo", null, Admin)).Expect(204);
    }

    // ------------------------------------------------------------------ what is stored where
    [Fact]
    public async Task The_file_goes_to_the_storage_folder_and_the_database_describes_it()
    {
        var user = await Data.UserAsync("Student");
        var bytes = Png(1234);

        var response = (await UploadAsync(user.Token, bytes, "holiday photo.png")).Expect(200);

        Assert.True(response["hasPhoto"].GetValue<bool>());
        Assert.Equal("image/png", response["contentType"].GetValue<string>());
        Assert.Equal(bytes.Length, response["sizeBytes"].GetValue<long>());
        Assert.Equal("/api/auth/me/photo", response["url"].GetValue<string>());

        var key = await Sql($"select \"StorageKey\" from identity.user_photos where \"UserId\" = '{user.UserId}'");
        Assert.Matches($@"^photos/{user.UserId:N}/[0-9a-f]{{32}}\.png$", key);
        Assert.Equal(bytes, await File.ReadAllBytesAsync(Path.Combine(Factory.UploadsPath, key!)));
        Assert.Equal("holiday photo.png", await Sql($"select \"OriginalFileName\" from identity.user_photos where \"UserId\" = '{user.UserId}'"));
        Assert.Equal("image/png", await Sql($"select \"ContentType\" from identity.user_photos where \"UserId\" = '{user.UserId}'"));
        Assert.Equal(bytes.Length.ToString(), await Sql($"select \"SizeBytes\" from identity.user_photos where \"UserId\" = '{user.UserId}'"));
    }

    [Fact]
    public async Task The_type_comes_from_the_bytes_not_from_the_name_the_client_gives()
    {
        var user = await Data.UserAsync("Student");

        // a PNG called ".exe" is a PNG; the name is only remembered
        (await UploadAsync(user.Token, Png(), "picture.exe")).Expect(200);

        var served = (await Api.DownloadAsync("/api/auth/me/photo", user.Token)).Expect(200);
        Assert.Equal("image/png", served.ContentType);
        Assert.EndsWith(".png", await Sql($"select \"StorageKey\" from identity.user_photos where \"UserId\" = '{user.UserId}'"));
    }

    [Fact]
    public async Task A_file_name_with_path_tricks_cannot_place_the_file_anywhere_else()
    {
        var user = await Data.UserAsync("Student");

        (await UploadAsync(user.Token, Png(), "../../../../etc/evil.png")).Expect(200);

        Assert.Equal("evil.png", await Sql($"select \"OriginalFileName\" from identity.user_photos where \"UserId\" = '{user.UserId}'"));   // directories stripped
        Assert.Equal(1, FilesOf(user.UserId));
        Assert.Matches(@"^[0-9a-f]{32}\.png$", Path.GetFileName(Directory.GetFiles(PhotosOf(user.UserId)).Single()));
    }

    // ------------------------------------------------------------------ serving
    [Fact]
    public async Task The_photo_is_served_with_safe_headers_and_can_be_cached_by_the_browser()
    {
        var user = await Data.UserAsync("Student");
        await UploadAsync(user.Token, Jpeg());

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me/photo");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user.Token);
        using var first = await Api.Http.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal("image/jpeg", first.Content.Headers.ContentType!.MediaType);
        Assert.Equal("nosniff", first.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Contains("private", first.Headers.CacheControl!.ToString());
        var etag = first.Headers.ETag;
        Assert.NotNull(etag);

        // the browser asks again with the tag it has: nothing is sent when the photo is unchanged
        using var again = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me/photo");
        again.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user.Token);
        again.Headers.IfNoneMatch.Add(etag!);
        using var cached = await Api.Http.SendAsync(again);
        Assert.Equal(HttpStatusCode.NotModified, cached.StatusCode);

        // after a change the old tag no longer matches
        await UploadAsync(user.Token, Jpeg(fill: 1));
        using var changed = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me/photo");
        changed.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user.Token);
        changed.Headers.IfNoneMatch.Add(etag!);
        using var fresh = await Api.Http.SendAsync(changed);
        Assert.Equal(HttpStatusCode.OK, fresh.StatusCode);
        Assert.NotEqual(etag, fresh.Headers.ETag);
    }

    [Fact]
    public async Task Me_tells_whether_there_is_a_photo_and_where_it_is()
    {
        var user = await Data.UserAsync("Student");

        var before = (await Api.GetAsync("/api/auth/me", user.Token)).Expect(200);
        Assert.False(before["hasPhoto"].GetValue<bool>());
        Assert.Null(before.Json!["photoUrl"]);

        await UploadAsync(user.Token, Png());

        var after = (await Api.GetAsync("/api/auth/me", user.Token)).Expect(200);
        Assert.True(after["hasPhoto"].GetValue<bool>());
        Assert.Equal("/api/auth/me/photo", after["photoUrl"].GetValue<string>());
    }

    // ------------------------------------------------------------------ replace and delete
    [Fact]
    public async Task A_new_photo_replaces_the_old_one_and_the_old_file_is_removed()
    {
        var user = await Data.UserAsync("Student");
        await UploadAsync(user.Token, Png(fill: 1));
        var oldFile = Directory.GetFiles(PhotosOf(user.UserId)).Single();

        var newBytes = Jpeg(500, fill: 2);
        (await UploadAsync(user.Token, newBytes, "new.jpg")).Expect(200);

        Assert.Equal("1", await Sql($"select count(*) from identity.user_photos where \"UserId\" = '{user.UserId}'"));   // still one photo
        Assert.False(File.Exists(oldFile));
        Assert.Equal(1, FilesOf(user.UserId));
        var served = (await Api.DownloadAsync("/api/auth/me/photo", user.Token)).Expect(200);
        Assert.Equal(newBytes, served.Content);
        Assert.Equal("image/jpeg", served.ContentType);
    }

    [Fact]
    public async Task Deleting_removes_the_file_and_the_row()
    {
        var user = await Data.UserAsync("Student");
        await UploadAsync(user.Token, Png());

        (await Api.DeleteAsync("/api/auth/me/photo", null, user.Token)).Expect(204);

        Assert.Equal(0, FilesOf(user.UserId));
        Assert.Equal("0", await Sql($"select count(*) from identity.user_photos where \"UserId\" = '{user.UserId}'"));
        (await Api.GetAsync("/api/auth/me/photo", user.Token)).Expect(404);
        (await Api.DeleteAsync("/api/auth/me/photo", null, user.Token)).Expect(404);
        Assert.False((await Api.GetAsync("/api/auth/me", user.Token))["hasPhoto"].GetValue<bool>());
    }

    [Fact]
    public async Task A_user_without_a_photo_gets_404()
    {
        var user = await Data.UserAsync("Student");

        (await Api.GetAsync("/api/auth/me/photo", user.Token)).Expect(404);
        (await Api.DeleteAsync("/api/auth/me/photo", null, user.Token)).Expect(404);
    }

    [Fact]
    public async Task A_photo_belongs_to_its_owner_only()
    {
        var owner = await Data.UserAsync("Student");
        var other = await Data.UserAsync("Student");
        await UploadAsync(owner.Token, Png(fill: 3));

        // the "me" endpoints always mean the caller: the other user sees and deletes nothing of the owner's
        (await Api.GetAsync("/api/auth/me/photo", other.Token)).Expect(404);
        (await Api.DeleteAsync("/api/auth/me/photo", null, other.Token)).Expect(404);
        Assert.Equal(1, FilesOf(owner.UserId));
    }

    // ------------------------------------------------------------------ what is refused
    [Fact]
    public async Task Pictures_that_are_not_jpeg_png_or_webp_are_refused_and_nothing_is_stored()
    {
        var user = await Data.UserAsync("Student");

        (await UploadAsync(user.Token, "GIF89a......"u8.ToArray(), "a.gif")).Expect(400);
        (await UploadAsync(user.Token, "<svg xmlns='http://www.w3.org/2000/svg'><script>alert(1)</script></svg>"u8.ToArray(), "a.svg")).Expect(400);
        (await UploadAsync(user.Token, "<html><script>alert(1)</script></html>"u8.ToArray(), "a.png")).Expect(400);   // a page pretending to be a picture
        (await UploadAsync(user.Token, "MZ......"u8.ToArray(), "photo.jpg")).Expect(400);                              // an executable pretending too
        (await UploadAsync(user.Token, "plain text"u8.ToArray(), "photo.png")).Expect(400);
        (await UploadAsync(user.Token, [], "empty.png")).Expect(400);

        Assert.Equal(0, FilesOf(user.UserId));
        Assert.Equal("0", await Sql($"select count(*) from identity.user_photos where \"UserId\" = '{user.UserId}'"));
    }

    [Fact]
    public async Task A_photo_over_five_megabytes_is_refused()
    {
        var user = await Data.UserAsync("Student");

        var refused = (await UploadAsync(user.Token, Png(6 * 1024 * 1024))).Expect(400);
        Assert.Contains("5 MB", refused.Raw);
        (await UploadAsync(user.Token, Png(4 * 1024 * 1024))).Expect(200);   // just under the limit is fine
    }

    [Fact]
    public async Task A_request_without_a_file_or_without_a_token_is_refused()
    {
        var user = await Data.UserAsync("Student");

        (await Api.UploadAsync("/api/auth/me/photo", "x.png", Png(), user.Token, "wrong-field", HttpMethod.Put)).Expect(400);
        (await Api.PutAsync("/api/auth/me/photo", new { }, user.Token)).Expect(415);
        (await UploadAsync(null, Png())).Expect(401);
        (await Api.GetAsync("/api/auth/me/photo")).Expect(401);
        (await Api.DeleteAsync("/api/auth/me/photo", null)).Expect(401);
    }

    [Fact]
    public async Task A_deactivated_account_can_no_longer_be_used_to_change_the_photo_after_relogin()
    {
        var studentId = await Data.StudentAsync();
        var user = await Data.StudentUserAsync(studentId);
        await UploadAsync(user.Token, Png());

        (await Api.PutAsync($"/api/students/{studentId}/status", new { status = "Withdrawn" }, Admin)).Expect(200);

        Assert.Equal(403, (await Api.PostAsync("/api/auth/login", new { email = user.Email, password = user.Password })).Code);
        Assert.Equal("1", await Sql($"select count(*) from identity.user_photos where \"UserId\" = '{user.UserId}'"));   // the photo is kept
    }

    // ------------------------------------------------------------------ staff see the photos of students and teachers
    [Fact]
    public async Task Staff_with_the_permission_see_a_students_photo_and_others_do_not()
    {
        var studentId = await Data.StudentAsync();
        var student = await Data.StudentUserAsync(studentId);
        var bytes = Png(400, 4);
        await UploadAsync(student.Token, bytes);
        var viewer = await Data.UserAsync("Admin", permissionIds: await Data.PermissionIdsAsync("CanViewStudents"));
        var plainAdmin = await Data.UserAsync("Admin");
        var teacher = await Data.TeacherUserAsync(await Data.TeacherAsync());

        Assert.Equal(bytes, (await Api.DownloadAsync($"/api/students/{studentId}/photo", viewer.Token)).Expect(200).Content);
        Assert.Equal(bytes, (await Api.DownloadAsync($"/api/students/{studentId}/photo", Admin)).Expect(200).Content);
        Assert.Equal("image/png", (await Api.DownloadAsync($"/api/students/{studentId}/photo", Admin)).ContentType);
        (await Api.DownloadAsync($"/api/students/{studentId}/photo", plainAdmin.Token)).Expect(403);
        (await Api.DownloadAsync($"/api/students/{studentId}/photo", teacher.Token)).Expect(403);
        (await Api.DownloadAsync($"/api/students/{studentId}/photo", student.Token)).Expect(403);   // a student uses /me/photo for their own
        (await Api.DownloadAsync($"/api/students/{studentId}/photo")).Expect(401);
    }

    [Fact]
    public async Task Staff_with_the_permission_see_a_teachers_photo()
    {
        var teacherId = await Data.TeacherAsync();
        var teacher = await Data.TeacherUserAsync(teacherId);
        var bytes = Jpeg(400, 6);
        await UploadAsync(teacher.Token, bytes);
        var viewer = await Data.UserAsync("Admin", permissionIds: await Data.PermissionIdsAsync("CanViewTeachers"));
        var studentViewer = await Data.UserAsync("Admin", permissionIds: await Data.PermissionIdsAsync("CanViewStudents"));

        Assert.Equal(bytes, (await Api.DownloadAsync($"/api/teachers/{teacherId}/photo", viewer.Token)).Expect(200).Content);
        (await Api.DownloadAsync($"/api/teachers/{teacherId}/photo", studentViewer.Token)).Expect(403);   // the students permission is not enough
    }

    [Fact]
    public async Task People_without_an_account_or_a_photo_have_nothing_to_show()
    {
        var noAccount = await Data.StudentAsync();
        var noPhoto = await Data.StudentAsync();
        await Data.StudentUserAsync(noPhoto);
        var noAccountTeacher = await Data.TeacherAsync();

        (await Api.DownloadAsync($"/api/students/{noAccount}/photo", Admin)).Expect(404);
        (await Api.DownloadAsync($"/api/students/{noPhoto}/photo", Admin)).Expect(404);
        (await Api.DownloadAsync($"/api/students/{Guid.NewGuid()}/photo", Admin)).Expect(404);
        (await Api.DownloadAsync($"/api/teachers/{noAccountTeacher}/photo", Admin)).Expect(404);
    }

    // ------------------------------------------------------------------ nothing is left half done
    // Saves work until the test arms it, so that the host can start and seed normally
    private sealed class FailingUnitOfWork(IIdentityUnitOfWork inner) : IIdentityUnitOfWork
    {
        public static volatile bool Armed;

        public Task<int> SaveChangesAsync(CancellationToken ct = default) =>
            Armed ? throw new InvalidOperationException("Simulated database failure.") : inner.SaveChangesAsync(ct);
    }

    private Api HostWithFailingDatabase()
    {
        var host = Factory.WithWebHostBuilder(b => b.ConfigureTestServices(s =>
        {
            var real = s.Last(d => d.ServiceType == typeof(IIdentityUnitOfWork)).ImplementationFactory!;   // the module's own registration
            s.RemoveAll<IIdentityUnitOfWork>();
            s.AddScoped<IIdentityUnitOfWork>(sp => new FailingUnitOfWork((IIdentityUnitOfWork)real(sp)));
        }));
        var client = host.CreateClient();
        FailingUnitOfWork.Armed = true;

        return new Api(client);
    }

    [Fact]
    public async Task If_the_database_fails_the_new_file_is_removed_and_no_row_is_left()
    {
        var user = await Data.UserAsync("Student");
        var failing = HostWithFailingDatabase();

        var response = await failing.UploadAsync("/api/auth/me/photo", "me.png", Png(), user.Token, method: HttpMethod.Put);

        Assert.Equal(500, response.Code);
        FailingUnitOfWork.Armed = false;
        Assert.Equal(0, FilesOf(user.UserId));
        Assert.Equal("0", await Sql($"select count(*) from identity.user_photos where \"UserId\" = '{user.UserId}'"));
    }

    [Fact]
    public async Task If_replacing_fails_the_old_photo_is_still_there()
    {
        var user = await Data.UserAsync("Student");
        var original = Png(300, 1);
        await UploadAsync(user.Token, original);
        var failing = HostWithFailingDatabase();

        Assert.Equal(500, (await failing.UploadAsync("/api/auth/me/photo", "new.jpg", Jpeg(), user.Token, method: HttpMethod.Put)).Code);

        FailingUnitOfWork.Armed = false;
        Assert.Equal(1, FilesOf(user.UserId));   // the failed new file was cleaned up, the old one kept
        Assert.Equal(original, (await Api.DownloadAsync("/api/auth/me/photo", user.Token)).Expect(200).Content);
    }

    [Fact]
    public async Task Many_uploads_at_the_same_moment_leave_exactly_one_photo_and_one_file()
    {
        var fresh = await Data.UserAsync("Student");
        var replacing = await Data.UserAsync("Student");
        await UploadAsync(replacing.Token, Png());

        var first = await Task.WhenAll(Enumerable.Range(0, 8).Select(i => UploadAsync(fresh.Token, Jpeg(fill: (byte)i))));
        var second = await Task.WhenAll(Enumerable.Range(0, 8).Select(i => UploadAsync(replacing.Token, Webp(100 + i))));

        Assert.All(first.Concat(second), r => Assert.True(r.Code == 200, $"{r.Code}: {r.Raw}"));
        foreach (var user in new[] { fresh, replacing })
        {
            Assert.Equal("1", await Sql($"select count(*) from identity.user_photos where \"UserId\" = '{user.UserId}'"));
            Assert.Equal(1, FilesOf(user.UserId));   // no file was left behind by the losers
        }
    }

    [Fact]
    public async Task The_photo_table_follows_the_rules_of_the_database()
    {
        var user = await Data.UserAsync("Student");
        await UploadAsync(user.Token, Png());

        // one photo per user is enforced by the database itself, not only by the code
        var duplicate = await Assert.ThrowsAnyAsync<Exception>(() => Factory.Database.ExecuteAsync(
            $"insert into identity.user_photos (\"Id\", \"UserId\", \"StorageKey\", \"OriginalFileName\", \"ContentType\", \"SizeBytes\", \"UploadedAt\") " +
            $"values (gen_random_uuid(), '{user.UserId}', 'photos/x/y.png', 'y.png', 'image/png', 1, now())"));
        Assert.Contains("duplicate key", duplicate.Message);

        // and a photo cannot exist for an account that does not
        var orphan = await Assert.ThrowsAnyAsync<Exception>(() => Factory.Database.ExecuteAsync(
            "insert into identity.user_photos (\"Id\", \"UserId\", \"StorageKey\", \"OriginalFileName\", \"ContentType\", \"SizeBytes\", \"UploadedAt\") " +
            "values (gen_random_uuid(), gen_random_uuid(), 'photos/x/y.png', 'y.png', 'image/png', 1, now())"));
        Assert.Contains("foreign key", orphan.Message);
    }
}
