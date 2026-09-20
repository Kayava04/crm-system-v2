namespace Crm.IntegrationTests.Tests;

public class MaterialsTests(CrmApiFactory factory) : ApiTest(factory)
{
    private const string Video = "https://www.youtube.com/watch?v=dQw4w9WgXcQ&t=5s";

    private static object Body(Guid course, string type, string title = "Title", string? body = null, string? url = null) =>
        new { courseId = course, type, title, description = (string?)null, body, url };

    [Fact]
    public async Task A_youtube_link_becomes_an_embeddable_video_with_a_thumbnail()
    {
        var teacher = await Data.TeacherUserAsync(await Data.TeacherAsync());

        var material = (await Api.PostAsync("/api/materials", Body(await Data.CourseAsync(), "Video", url: Video), teacher.Token)).Expect(201);

        Assert.Equal("dQw4w9WgXcQ", material["youTubeVideoId"].GetValue<string>());
        Assert.Equal("https://www.youtube-nocookie.com/embed/dQw4w9WgXcQ", material["embedUrl"].GetValue<string>());
        Assert.Equal("https://img.youtube.com/vi/dQw4w9WgXcQ/hqdefault.jpg", material["thumbnailUrl"].GetValue<string>());
        Assert.Null(material.Json!["url"]);
    }

    [Fact]
    public async Task Bad_videos_articles_and_links_are_rejected()
    {
        var course = await Data.CourseAsync();

        (await Api.PostAsync("/api/materials", Body(course, "Video", url: "https://evil.com/watch?v=dQw4w9WgXcQ"), Admin)).Expect(400);
        (await Api.PostAsync("/api/materials", Body(course, "Video", url: "https://vimeo.com/1"), Admin)).Expect(400);
        (await Api.PostAsync("/api/materials", Body(course, "Article", body: ""), Admin)).Expect(400);
        (await Api.PostAsync("/api/materials", Body(course, "Link", url: "javascript:alert(1)"), Admin)).Expect(400);
        (await Api.PostAsync("/api/materials", Body(course, "Link", title: "", url: "https://example.com"), Admin)).Expect(400);
        (await Api.PostAsync("/api/materials", Body(Guid.NewGuid(), "Link", url: "https://example.com"), Admin)).Expect(404);
    }

    [Fact]
    public async Task Only_the_field_of_the_material_type_is_stored()
    {
        var course = await Data.CourseAsync();

        var article = (await Api.PostAsync("/api/materials", Body(course, "Article", body: "  # Hello  ", url: "https://ignored.example"), Admin)).Expect(201);
        var link = (await Api.PostAsync("/api/materials", Body(course, "Link", body: "ignored", url: "https://example.com/page"), Admin)).Expect(201);

        Assert.Equal("# Hello", article["body"].GetValue<string>());
        Assert.Null(article.Json!["url"]);
        Assert.Equal("https://example.com/page", link["url"].GetValue<string>());
        Assert.Null(link.Json!["body"]);
    }

    [Fact]
    public async Task Students_can_read_but_not_write()
    {
        var course = await Data.CourseAsync();
        var teacher = await Data.TeacherUserAsync(await Data.TeacherAsync());
        var student = await Data.StudentUserAsync(await Data.StudentAsync());
        var material = (await Api.PostAsync("/api/materials", Body(course, "Link", url: "https://example.com"), teacher.Token)).Expect(201);

        (await Api.PostAsync("/api/materials", Body(course, "Link", url: "https://example.com"), student.Token)).Expect(403);
        (await Api.GetAsync($"/api/materials/{material.Id}", student.Token)).Expect(200);
        (await Api.GetAsync($"/api/materials?courseId={course}", student.Token)).Expect(200);
        (await Api.PutAsync($"/api/materials/{material.Id}", Body(course, "Link", url: "https://x.y"), student.Token)).Expect(403);
        (await Api.DeleteAsync($"/api/materials/{material.Id}", null, student.Token)).Expect(403);
    }

    [Fact]
    public async Task Only_the_author_or_an_admin_can_change_a_material()
    {
        var course = await Data.CourseAsync();
        var author = await Data.TeacherUserAsync(await Data.TeacherAsync());
        var colleague = await Data.TeacherUserAsync(await Data.TeacherAsync());
        var material = (await Api.PostAsync("/api/materials", Body(course, "Video", url: Video), author.Token)).Expect(201);
        var update = new { type = "Article", title = "Changed", description = (string?)null, body = "now text", url = (string?)null };

        (await Api.PutAsync($"/api/materials/{material.Id}", update, colleague.Token)).Expect(403);
        (await Api.DeleteAsync($"/api/materials/{material.Id}", null, colleague.Token)).Expect(403);

        (await Api.PutAsync($"/api/materials/{material.Id}", update, author.Token)).Expect(204);
        var changed = (await Api.GetAsync($"/api/materials/{material.Id}", Admin)).Expect(200);
        Assert.Equal("Article", changed["type"].GetValue<string>());
        Assert.Null(changed.Json!["youTubeVideoId"]);   // the old video is gone
        Assert.Null(changed.Json!["embedUrl"]);

        (await Api.DeleteAsync($"/api/materials/{material.Id}", null, Admin)).Expect(204);     // an admin may delete a foreign material
        (await Api.GetAsync($"/api/materials/{material.Id}", Admin)).Expect(404);
        (await Api.DeleteAsync($"/api/materials/{material.Id}", null, Admin)).Expect(404);
    }

    [Fact]
    public async Task Materials_can_be_filtered_by_course_type_and_title_and_paged()
    {
        var course = await Data.CourseAsync();
        var tag = "Zq" + TestData.Unique();
        await Api.PostAsync("/api/materials", Body(course, "Video", tag + " video", url: Video), Admin);
        await Api.PostAsync("/api/materials", Body(course, "Article", tag + " article", body: "text"), Admin);
        await Api.PostAsync("/api/materials", Body(await Data.CourseAsync(), "Link", tag + " link", url: "https://example.com"), Admin);

        Assert.Equal(2, (await Api.GetAsync($"/api/materials?courseId={course}", Admin))["totalCount"].GetValue<int>());
        Assert.Equal(1, (await Api.GetAsync($"/api/materials?courseId={course}&type=Video", Admin))["totalCount"].GetValue<int>());
        Assert.Equal(3, (await Api.GetAsync($"/api/materials?search={tag}", Admin))["totalCount"].GetValue<int>());
        Assert.Equal(1, (await Api.GetAsync($"/api/materials?search={tag}%20ARTICLE", Admin))["totalCount"].GetValue<int>());   // case-insensitive
        Assert.Equal(2, (await Api.GetAsync($"/api/materials?search={tag}&pageSize=2", Admin)).Items.Count);

        var video = (await Api.GetAsync($"/api/materials?courseId={course}&type=Video", Admin)).Items.Single()!;
        Assert.NotNull(video["thumbnailUrl"]);
    }
}
