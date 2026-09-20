namespace Crm.IntegrationTests.Tests;

public class MasterDataTests(CrmApiFactory factory) : ApiTest(factory)
{
    // ------------------------------------------------------------------ students
    [Fact]
    public async Task A_student_can_be_created_read_updated_and_commented()
    {
        var email = TestData.Email("student");
        var created = (await Api.PostAsync("/api/students", Data.StudentBody(email: email, lastName: "Petrenko"), Admin)).Expect(201);

        var detail = (await Api.GetAsync($"/api/students/{created.Id}", Admin)).Expect(200);
        Assert.Equal(email, detail["email"].GetValue<string>());
        Assert.Equal("Active", detail["status"].GetValue<string>());
        Assert.False(detail["hasAccount"].GetValue<bool>());

        (await Api.PutAsync($"/api/students/{created.Id}", new
        {
            firstName = "Ivan", lastName = "Updated", middleName = (string?)null, dateOfBirth = "1999-09-09",
            phoneNumber = "+380509998877", city = "Lviv", country = "Ukraine", isChild = false
        }, Admin)).Expect(204);
        (await Api.PatchAsync($"/api/students/{created.Id}/comment", new { comment = "good student" }, Admin)).Expect(204);

        var updated = (await Api.GetAsync($"/api/students/{created.Id}", Admin)).Expect(200);
        Assert.Equal("Updated", updated["lastName"].GetValue<string>());
        Assert.Equal("Lviv", updated["city"].GetValue<string>());
        Assert.Equal("good student", updated["comment"].GetValue<string>());
        (await Api.GetAsync($"/api/students/{Guid.NewGuid()}", Admin)).Expect(404);
    }

    [Fact]
    public async Task Student_emails_are_unique_ignoring_case_and_input_is_validated()
    {
        var email = TestData.Email("student");
        (await Api.PostAsync("/api/students", Data.StudentBody(email: email), Admin)).Expect(201);

        (await Api.PostAsync("/api/students", Data.StudentBody(email: email.ToUpperInvariant()), Admin)).Expect(409);
        (await Api.PostAsync("/api/students", Data.StudentBody(email: "not-an-email"), Admin)).Expect(400);
    }

    [Fact]
    public async Task Students_are_listed_with_filters_and_paging()
    {
        var tag = "Zq" + TestData.Unique();
        for (var i = 0; i < 3; i++)
            (await Api.PostAsync("/api/students", Data.StudentBody(lastName: tag), Admin)).Expect(201);

        var all = (await Api.GetAsync($"/api/students?search={tag}", Admin)).Expect(200);
        Assert.Equal(3, all["totalCount"].GetValue<int>());

        var page = (await Api.GetAsync($"/api/students?search={tag}&pageSize=2&page=2", Admin)).Expect(200);
        Assert.Single(page.Items);
        Assert.Equal(2, page["totalPages"].GetValue<int>());

        Assert.Equal(0, (await Api.GetAsync($"/api/students?search={tag}&city=Nowhere", Admin))["totalCount"].GetValue<int>());
    }

    [Fact]
    public async Task Student_preferences_and_parent_info_can_be_changed()
    {
        var student = (await Api.PostAsync("/api/students", Data.StudentBody(isChild: true), Admin)).Expect(201).Id;

        (await Api.PutAsync($"/api/students/{student}/preferences", new
        {
            learningGoal = "Study", format = "Offline", lessonType = "Group", intensity = 5,
            currentLevel = "B2", hadPreviousCourses = true, languages = new[] { "German", "French" }
        }, Admin)).Expect(204);
        (await Api.PostAsync($"/api/students/{student}/parent-info", new
        {
            firstName = "Olga", lastName = "Parent", middleName = (string?)null, phoneNumber = "+380501234567", email = TestData.Email("parent")
        }, Admin)).Expect(201);

        var detail = (await Api.GetAsync($"/api/students/{student}", Admin)).Expect(200);
        Assert.Equal("Study", detail["preferences"]!["learningGoal"]!.GetValue<string>());
        Assert.Equal(2, detail["languages"].AsArray().Count);
        Assert.Equal("Olga", detail["parentInfo"]!["firstName"]!.GetValue<string>());

        (await Api.DeleteAsync($"/api/students/{student}/parent-info", null, Admin)).Expect(204);
        Assert.Null((await Api.GetAsync($"/api/students/{student}", Admin)).Expect(200).Json!["parentInfo"]);
    }

    // ------------------------------------------------------------------ teachers
    [Fact]
    public async Task Parent_info_is_only_for_children()
    {
        var adult = await Data.StudentAsync();

        (await Api.PostAsync($"/api/students/{adult}/parent-info", new
        {
            firstName = "Olga", lastName = "Parent", middleName = (string?)null, phoneNumber = "+380501234567", email = TestData.Email("parent")
        }, Admin)).Expect(409);
    }

    // ------------------------------------------------------------------ teachers
    [Fact]
    public async Task A_teacher_is_created_with_a_first_salary_rate_and_a_later_one_takes_over_when_its_date_comes()
    {
        var teacher = await Data.TeacherAsync(baseSalary: 1500, lessonsRate: 150);

        var detail = (await Api.GetAsync($"/api/teachers/{teacher}", Admin)).Expect(200);
        Assert.Equal("Probation", detail["status"].GetValue<string>());
        Assert.Equal(1500m, detail["currentSalaryRate"]!["baseSalary"]!.GetValue<decimal>());

        // a new rate can start tomorrow at the earliest; until then the first one applies
        (await Api.PostAsync($"/api/teachers/{teacher}/salary-rates", new { baseSalary = 2000, lessonsRate = 200, effectiveFrom = DateTime.UtcNow.AddDays(1) }, Admin)).Expect(204);
        Assert.Equal(1500m, (await Api.GetAsync($"/api/teachers/{teacher}", Admin))["currentSalaryRate"]!["baseSalary"]!.GetValue<decimal>());

        // tomorrow arrives
        await Factory.Database.ExecuteAsync($@"update teachers.teacher_salary_rates set ""EffectiveFrom"" = ""EffectiveFrom"" - interval '2 days' where ""TeacherId"" = '{teacher}'");
        Assert.Equal(2000m, (await Api.GetAsync($"/api/teachers/{teacher}", Admin))["currentSalaryRate"]!["baseSalary"]!.GetValue<decimal>());
    }

    [Fact]
    public async Task Salary_rate_dates_cannot_be_in_the_past_or_repeat_a_day()
    {
        var teacher = await Data.TeacherAsync();

        (await Api.PostAsync($"/api/teachers/{teacher}/salary-rates", new { baseSalary = 2000, lessonsRate = 200, effectiveFrom = DateTime.UtcNow.AddDays(-1) }, Admin)).Expect(400);
        (await Api.PostAsync($"/api/teachers/{teacher}/salary-rates", new { baseSalary = 2000, lessonsRate = 200, effectiveFrom = DateTime.UtcNow }, Admin)).Expect(409);   // the first rate already starts today
        (await Api.PostAsync($"/api/teachers/{teacher}/salary-rates", new { baseSalary = 2000, lessonsRate = 200, effectiveFrom = DateTime.UtcNow.AddDays(5) }, Admin)).Expect(204);
        (await Api.PostAsync($"/api/teachers/{teacher}/salary-rates", new { baseSalary = 3000, lessonsRate = 300, effectiveFrom = DateTime.UtcNow.AddDays(5) }, Admin)).Expect(409);
        (await Api.PostAsync($"/api/teachers/{Guid.NewGuid()}/salary-rates", new { baseSalary = 1, lessonsRate = 1, effectiveFrom = DateTime.UtcNow.AddDays(5) }, Admin)).Expect(404);
    }

    [Fact]
    public async Task Teacher_emails_are_unique_and_a_teacher_can_be_updated()
    {
        var email = TestData.Email("teacher");
        var teacher = (await Api.PostAsync("/api/teachers", Data.TeacherBody(email: email), Admin)).Expect(201).Id;

        (await Api.PostAsync("/api/teachers", Data.TeacherBody(email: email), Admin)).Expect(409);
        (await Api.PutAsync($"/api/teachers/{teacher}", new
        {
            firstName = "Renamed", lastName = "Teacher", middleName = (string?)null, dateOfBirth = "1980-01-01",
            phoneNumber = "+380501110000", city = "Odesa", country = "Ukraine"
        }, Admin)).Expect(204);

        Assert.Equal("Odesa", (await Api.GetAsync($"/api/teachers/{teacher}", Admin))["city"].GetValue<string>());
    }

    [Fact]
    public async Task Teacher_status_changes_are_recorded()
    {
        var teacher = await Data.TeacherAsync();

        (await Api.PutAsync($"/api/teachers/{teacher}/status", new { status = "Employed" }, Admin)).Expect(200);
        Assert.Equal("Employed", (await Api.GetAsync($"/api/teachers/{teacher}", Admin))["status"].GetValue<string>());
        (await Api.PutAsync($"/api/teachers/{teacher}/status", new { status = "Employed" }, Admin)).Expect(409);
    }

    // ------------------------------------------------------------------ courses
    [Fact]
    public async Task Courses_can_be_updated_archived_activated_and_deleted()
    {
        var course = await Data.CourseAsync();

        (await Api.PutAsync($"/api/courses/{course}", new
        {
            name = "Renamed " + TestData.Unique(), language = "French", level = "C1", format = "Offline", lessonType = "Group",
            durationMonths = 6, lessonsCount = 40, lessonsPerWeek = 3, price = 5000, description = "d"
        }, Admin)).Expect(204);
        var detail = (await Api.GetAsync($"/api/courses/{course}", Admin)).Expect(200);
        Assert.Equal("French", detail["language"].GetValue<string>());
        Assert.Equal(5000m, detail["price"].GetValue<decimal>());

        (await Api.PutAsync($"/api/courses/{course}/archive", null, Admin)).Expect(204);
        (await Api.PutAsync($"/api/courses/{course}/archive", null, Admin)).Expect(409);
        Assert.Equal("Archived", (await Api.GetAsync($"/api/courses/{course}", Admin))["status"].GetValue<string>());
        (await Api.PutAsync($"/api/courses/{course}/activate", null, Admin)).Expect(204);

        (await Api.DeleteAsync($"/api/courses/{course}", null, Admin)).Expect(204);
        (await Api.GetAsync($"/api/courses/{course}", Admin)).Expect(404);
    }

    [Fact]
    public async Task Course_names_are_unique_and_the_list_filters()
    {
        var name = "Unique course " + TestData.Unique();
        object Body(string n) => new { name = n, language = "English", level = "A1", format = "Online", lessonType = "Individual", durationMonths = 3, lessonsCount = 10, lessonsPerWeek = 2, price = 100, description = (string?)null };

        (await Api.PostAsync("/api/courses", Body(name), Admin)).Expect(201);
        (await Api.PostAsync("/api/courses", Body(name), Admin)).Expect(409);
        (await Api.PostAsync("/api/courses", new { name = "x", language = "English", level = "A1", format = "Online", lessonType = "Individual", durationMonths = 0, lessonsCount = 10, lessonsPerWeek = 2, price = 100 }, Admin)).Expect(400);

        Assert.Equal(1, (await Api.GetAsync($"/api/courses?search={name.Replace(" ", "%20")}", Admin))["totalCount"].GetValue<int>());
    }

    [Fact]
    public async Task Master_data_needs_the_matching_permissions()
    {
        var student = await Data.StudentUserAsync(await Data.StudentAsync());

        (await Api.PostAsync("/api/students", Data.StudentBody(), student.Token)).Expect(403);
        (await Api.PostAsync("/api/teachers", Data.TeacherBody(), student.Token)).Expect(403);
        (await Api.PostAsync("/api/courses", new { }, student.Token)).Expect(403);
        (await Api.GetAsync("/api/courses", student.Token)).Expect(403);
    }
}
