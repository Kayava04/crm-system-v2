namespace Crm.IntegrationTests.Tests;

public class BulkTests(CrmApiFactory factory) : ApiTest(factory)
{
    private static JsonNode Result(ApiResponse response, int index) => response["results"].AsArray()[index]!;

    // ------------------------------------------------------------------ students: create
    [Fact]
    public async Task Bulk_creation_saves_the_valid_students_and_reports_each_item()
    {
        var existing = TestData.Email("existing");
        (await Api.PostAsync("/api/students", Data.StudentBody(email: existing), Admin)).Expect(201);
        var repeated = TestData.Email("repeated");

        var response = (await Api.PostAsync("/api/students/bulk", new
        {
            students = new[]
            {
                Data.StudentBody(),                                  // 0 valid
                Data.StudentBody(email: "not-an-email"),             // 1 invalid email
                Data.StudentBody(email: repeated),                   // 2 valid
                Data.StudentBody(email: repeated.ToUpperInvariant()),// 3 repeated inside the request
                Data.StudentBody(email: existing),                   // 4 already in the system
                Data.StudentBody(),                                  // 5 valid
            }
        }, Admin)).Expect(200);

        Assert.Equal(6, response["total"].GetValue<int>());
        Assert.Equal(3, response["succeeded"].GetValue<int>());
        Assert.Equal(3, response["failed"].GetValue<int>());

        Assert.True(Result(response, 0)["success"]!.GetValue<bool>());
        Assert.Contains("Email", string.Join(' ', Result(response, 1)["errors"]!.AsArray().Select(e => e!.GetValue<string>())));
        Assert.True(Result(response, 2)["success"]!.GetValue<bool>());
        Assert.Contains("repeated in this request", Result(response, 3)["errors"]![0]!.GetValue<string>());
        Assert.Contains("already exists", Result(response, 4)["errors"]![0]!.GetValue<string>());
        Assert.Equal(new[] { 0, 1, 2, 3, 4, 5 }, response["results"].AsArray().Select(r => r!["index"]!.GetValue<int>()));

        // the saved students are complete: preferences and languages come with them
        var id = Result(response, 0)["id"]!.GetValue<Guid>();
        var detail = (await Api.GetAsync($"/api/students/{id}", Admin)).Expect(200);
        Assert.Equal("Work", detail["preferences"]!["learningGoal"]!.GetValue<string>());
        Assert.Single(detail["languages"].AsArray());
        Assert.Null(Result(response, 1)["id"]);
    }

    [Fact]
    public async Task With_all_or_nothing_one_invalid_student_saves_nobody()
    {
        var lastName = "Zq" + TestData.Unique();

        var response = (await Api.PostAsync("/api/students/bulk", new
        {
            allOrNothing = true,
            students = new[] { Data.StudentBody(lastName: lastName), Data.StudentBody(email: "broken"), Data.StudentBody(lastName: lastName) }
        }, Admin)).Expect(400);

        Assert.Equal(0, response["succeeded"].GetValue<int>());
        Assert.Equal(3, response["failed"].GetValue<int>());
        Assert.Contains("all-or-nothing", Result(response, 0)["errors"]![0]!.GetValue<string>());   // valid item: explained
        Assert.Contains("Email", string.Join(' ', Result(response, 1)["errors"]!.AsArray().Select(e => e!.GetValue<string>())));
        Assert.Equal(0, (await Api.GetAsync($"/api/students?search={lastName}", Admin))["totalCount"].GetValue<int>());   // nobody was saved
    }

    [Fact]
    public async Task With_all_or_nothing_a_clean_batch_is_saved_completely()
    {
        var lastName = "Zq" + TestData.Unique();

        var response = (await Api.PostAsync("/api/students/bulk", new
        {
            allOrNothing = true,
            students = Enumerable.Range(0, 25).Select(_ => Data.StudentBody(lastName: lastName)).ToArray()
        }, Admin)).Expect(200);

        Assert.Equal(25, response["succeeded"].GetValue<int>());
        Assert.Equal(25, (await Api.GetAsync($"/api/students?search={lastName}&pageSize=100", Admin))["totalCount"].GetValue<int>());
    }

    [Fact]
    public async Task Bulk_creation_handles_a_large_batch()
    {
        var lastName = "Zq" + TestData.Unique();

        var response = (await Api.PostAsync("/api/students/bulk", new
        {
            students = Enumerable.Range(0, 500).Select(_ => Data.StudentBody(lastName: lastName)).ToArray()
        }, Admin)).Expect(200);

        Assert.Equal(500, response["succeeded"].GetValue<int>());
        Assert.Equal("500", await Sql($"select count(*) from students.students where \"LastName\" = '{lastName}'"));
    }

    [Fact]
    public async Task Bulk_requests_have_size_limits_and_need_permissions()
    {
        (await Api.PostAsync("/api/students/bulk", new { students = Array.Empty<object>() }, Admin)).Expect(400);
        (await Api.PostAsync("/api/students/bulk", new { students = Enumerable.Range(0, 501).Select(_ => Data.StudentBody()).ToArray() }, Admin)).Expect(400);
        (await Api.PostAsync("/api/teachers/bulk", new { teachers = Array.Empty<object>() }, Admin)).Expect(400);
        (await Api.DeleteAsync("/api/students/bulk", new { ids = Array.Empty<Guid>() }, Admin)).Expect(400);
        (await Api.DeleteAsync("/api/teachers/bulk", new { ids = Enumerable.Range(0, 501).Select(_ => Guid.NewGuid()).ToArray() }, Admin)).Expect(400);

        var teacher = await Data.TeacherUserAsync(await Data.TeacherAsync());
        var student = await Data.StudentUserAsync(await Data.StudentAsync());
        (await Api.PostAsync("/api/students/bulk", new { students = new[] { Data.StudentBody() } }, teacher.Token)).Expect(403);
        (await Api.PostAsync("/api/teachers/bulk", new { teachers = new[] { Data.TeacherBody() } }, student.Token)).Expect(403);
        (await Api.DeleteAsync("/api/students/bulk", new { ids = new[] { Guid.NewGuid() } }, student.Token)).Expect(403);
        (await Api.DeleteAsync("/api/teachers/bulk", new { ids = new[] { Guid.NewGuid() } }, teacher.Token)).Expect(403);
        (await Api.PostAsync("/api/students/bulk", new { students = new[] { Data.StudentBody() } })).Expect(401);
    }

    // ------------------------------------------------------------------ students: delete
    [Fact]
    public async Task Bulk_delete_removes_empty_students_and_refuses_the_ones_with_history()
    {
        var plainA = await Data.StudentAsync();
        var plainB = await Data.StudentAsync();
        var enrolled = await Data.StudentAsync();
        await Data.EnrollmentAsync(enrolled, await Data.CourseAsync());
        var withAccount = await Data.StudentAsync();
        await Data.StudentUserAsync(withAccount);
        var unknown = Guid.NewGuid();

        var response = (await Api.DeleteAsync("/api/students/bulk", new { ids = new[] { plainA, enrolled, withAccount, unknown, plainB, plainA } }, Admin)).Expect(200);

        Assert.Equal(6, response["total"].GetValue<int>());
        Assert.Equal(2, response["succeeded"].GetValue<int>());
        Assert.True(Result(response, 0)["success"]!.GetValue<bool>());
        Assert.Contains("Withdrawn", Result(response, 1)["errors"]![0]!.GetValue<string>());
        Assert.Contains("Withdrawn", Result(response, 2)["errors"]![0]!.GetValue<string>());
        Assert.Contains("not found", Result(response, 3)["errors"]![0]!.GetValue<string>());
        Assert.True(Result(response, 4)["success"]!.GetValue<bool>());
        Assert.Contains("repeated", Result(response, 5)["errors"]![0]!.GetValue<string>());

        (await Api.GetAsync($"/api/students/{plainA}", Admin)).Expect(404);
        (await Api.GetAsync($"/api/students/{plainB}", Admin)).Expect(404);
        (await Api.GetAsync($"/api/students/{enrolled}", Admin)).Expect(200);      // kept
        (await Api.GetAsync($"/api/students/{withAccount}", Admin)).Expect(200);   // kept
    }

    // ------------------------------------------------------------------ teachers
    [Fact]
    public async Task Bulk_teacher_creation_reports_each_item_and_keeps_the_first_salary_rate()
    {
        var existing = TestData.Email("existing");
        (await Api.PostAsync("/api/teachers", Data.TeacherBody(email: existing), Admin)).Expect(201);

        var response = (await Api.PostAsync("/api/teachers/bulk", new
        {
            teachers = new[]
            {
                Data.TeacherBody(baseSalary: 1700, lessonsRate: 170),
                Data.TeacherBody(email: "broken"),
                Data.TeacherBody(email: existing),
                Data.TeacherBody(),
            }
        }, Admin)).Expect(200);

        Assert.Equal(4, response["total"].GetValue<int>());
        Assert.Equal(2, response["succeeded"].GetValue<int>());
        Assert.False(Result(response, 1)["success"]!.GetValue<bool>());
        Assert.Contains("already exists", Result(response, 2)["errors"]![0]!.GetValue<string>());

        var detail = (await Api.GetAsync($"/api/teachers/{Result(response, 0)["id"]!.GetValue<Guid>()}", Admin)).Expect(200);
        Assert.Equal("Probation", detail["status"].GetValue<string>());
        Assert.Equal(1700m, detail["currentSalaryRate"]!["baseSalary"]!.GetValue<decimal>());
    }

    [Fact]
    public async Task With_all_or_nothing_one_invalid_teacher_saves_nobody()
    {
        var lastName = "Zq" + TestData.Unique();

        (await Api.PostAsync("/api/teachers/bulk", new
        {
            allOrNothing = true,
            teachers = new[] { Data.TeacherBody(lastName: lastName), Data.TeacherBody(email: "broken", lastName: lastName) }
        }, Admin)).Expect(400);

        Assert.Equal("0", await Sql($"select count(*) from teachers.teachers where \"LastName\" = '{lastName}'"));
    }

    [Fact]
    public async Task Bulk_teacher_delete_follows_the_same_rules_as_a_single_delete()
    {
        var plain = await Data.TeacherAsync();
        var busy = await Data.TeacherAsync();
        (await Data.GenerateAsync(enrollmentId: await Data.EnrollmentAsync(await Data.StudentAsync(), await Data.CourseAsync()), teacherId: busy)).Expect(201);
        var withAccount = await Data.TeacherAsync();
        await Data.TeacherUserAsync(withAccount);

        var response = (await Api.DeleteAsync("/api/teachers/bulk", new { ids = new[] { plain, busy, withAccount, Guid.NewGuid() } }, Admin)).Expect(200);

        Assert.Equal(1, response["succeeded"].GetValue<int>());
        Assert.True(Result(response, 0)["success"]!.GetValue<bool>());
        Assert.Contains("Dismissed", Result(response, 1)["errors"]![0]!.GetValue<string>());
        Assert.Contains("Dismissed", Result(response, 2)["errors"]![0]!.GetValue<string>());
        Assert.Contains("not found", Result(response, 3)["errors"]![0]!.GetValue<string>());
        (await Api.GetAsync($"/api/teachers/{plain}", Admin)).Expect(404);
        (await Api.GetAsync($"/api/teachers/{busy}", Admin)).Expect(200);
    }
}
