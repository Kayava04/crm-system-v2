namespace Crm.IntegrationTests.Tests;

public class EnrollmentTests(CrmApiFactory factory) : ApiTest(factory)
{
    [Fact]
    public async Task Enrollment_takes_the_course_price_and_calculates_the_end_date()
    {
        var student = await Data.StudentAsync();
        var course = await Data.CourseAsync(price: 3000);

        var created = (await Api.PostAsync("/api/enrollments", new
        {
            studentId = student, courseId = course, startDate = "2030-01-31",
            discountedPrice = (decimal?)null, comment = (string?)null, preferredSchedule = "Tue/Thu after 18:00"
        }, Admin)).Expect(201);

        Assert.Equal(3000m, created["coursePrice"].GetValue<decimal>());
        Assert.Equal(3000m, created["effectivePrice"].GetValue<decimal>());
        Assert.Equal("2030-04-30", created["endDate"].GetValue<string>());   // start + the course's 3 months
        Assert.StartsWith("CTR-20300131-", created["enrollmentNumber"].GetValue<string>());

        var detail = (await Api.GetAsync($"/api/enrollments/{created.Id}", Admin)).Expect(200);
        Assert.Equal("Draft", detail["status"].GetValue<string>());
        Assert.Equal("Tue/Thu after 18:00", detail["preferredSchedule"].GetValue<string>());
    }

    [Fact]
    public async Task Enrollment_numbers_count_up_within_a_day()
    {
        var course = await Data.CourseAsync();
        var numbers = new List<string>();

        for (var i = 0; i < 3; i++)
        {
            var created = (await Api.PostAsync("/api/enrollments", new
            {
                studentId = await Data.StudentAsync(), courseId = course, startDate = "2031-02-03",
                discountedPrice = (decimal?)null, comment = (string?)null, preferredSchedule = (string?)null
            }, Admin)).Expect(201);
            numbers.Add(created["enrollmentNumber"].GetValue<string>());
        }

        Assert.Equal(["CTR-20310203-0001", "CTR-20310203-0002", "CTR-20310203-0003"], numbers);
    }

    [Fact]
    public async Task Discount_can_be_applied_removed_and_a_price_change_clears_it()
    {
        var enrollment = await Data.EnrollmentAsync(await Data.StudentAsync(), await Data.CourseAsync(price: 3000), activate: false);

        (await Api.PutAsync($"/api/enrollments/{enrollment}/discount", new { discountedPrice = 3000 }, Admin)).Expect(409);   // must be lower
        (await Api.PutAsync($"/api/enrollments/{enrollment}/discount", new { discountedPrice = 2000 }, Admin)).Expect(204);
        Assert.Equal(2000m, (await Api.GetAsync($"/api/enrollments/{enrollment}", Admin))["effectivePrice"].GetValue<decimal>());

        (await Api.PutAsync($"/api/enrollments/{enrollment}/price", new { coursePrice = 3500 }, Admin)).Expect(204);
        var detail = (await Api.GetAsync($"/api/enrollments/{enrollment}", Admin)).Expect(200);
        Assert.Equal(3500m, detail["effectivePrice"].GetValue<decimal>());
        Assert.Null(detail.Json!["discountedPrice"]);

        (await Api.DeleteAsync($"/api/enrollments/{enrollment}/discount", null, Admin)).Expect(409);   // nothing to remove
    }

    [Fact]
    public async Task Same_student_cannot_have_two_open_enrollments_in_one_course()
    {
        var student = await Data.StudentAsync();
        var course = await Data.CourseAsync();
        await Data.EnrollmentAsync(student, course);

        var second = await Api.PostAsync("/api/enrollments", new
        {
            studentId = student, courseId = course, startDate = Today,
            discountedPrice = (decimal?)null, comment = (string?)null, preferredSchedule = (string?)null
        }, Admin);

        second.Expect(409);
    }

    [Fact]
    public async Task Unknown_student_or_course_is_not_found()
    {
        var student = await Data.StudentAsync();
        var course = await Data.CourseAsync();
        object Body(Guid s, Guid c) => new { studentId = s, courseId = c, startDate = Today, discountedPrice = (decimal?)null, comment = (string?)null, preferredSchedule = (string?)null };

        (await Api.PostAsync("/api/enrollments", Body(Guid.NewGuid(), course), Admin)).Expect(404);
        (await Api.PostAsync("/api/enrollments", Body(student, Guid.NewGuid()), Admin)).Expect(404);
    }

    [Fact]
    public async Task Status_changes_follow_the_rules_and_report_double_changes()
    {
        var enrollment = await Data.EnrollmentAsync(await Data.StudentAsync(), await Data.CourseAsync(), activate: false);

        (await Api.PutAsync($"/api/enrollments/{enrollment}/activate", null, Admin)).Expect(200);
        (await Api.PutAsync($"/api/enrollments/{enrollment}/activate", null, Admin)).Expect(409);
        (await Api.PutAsync($"/api/enrollments/{enrollment}/suspend", null, Admin)).Expect(200);
        (await Api.PutAsync($"/api/enrollments/{enrollment}/suspend", null, Admin)).Expect(409);
        (await Api.PutAsync($"/api/enrollments/{enrollment}/terminate", null, Admin)).Expect(200);
        (await Api.PutAsync($"/api/enrollments/{enrollment}/terminate", null, Admin)).Expect(409);
        (await Api.PutAsync($"/api/enrollments/{Guid.NewGuid()}/activate", null, Admin)).Expect(404);
    }

    [Fact]
    public async Task List_filters_by_student_course_and_status_and_pages()
    {
        var student = await Data.StudentAsync();
        var course = await Data.CourseAsync();
        var enrollment = await Data.EnrollmentAsync(student, course);

        var byStudent = (await Api.GetAsync($"/api/enrollments?studentId={student}", Admin)).Expect(200);
        Assert.Equal(1, byStudent["totalCount"].GetValue<int>());
        Assert.Equal(enrollment, Guid.Parse(byStudent.Items[0]!["id"]!.GetValue<string>()));

        Assert.Equal(1, (await Api.GetAsync($"/api/enrollments?courseId={course}&status=Active", Admin))["totalCount"].GetValue<int>());
        Assert.Equal(0, (await Api.GetAsync($"/api/enrollments?courseId={course}&status=Draft", Admin))["totalCount"].GetValue<int>());
        Assert.Equal(100, (await Api.GetAsync($"/api/enrollments?studentId={student}&pageSize=1000", Admin))["pageSize"].GetValue<int>());   // page size is capped
    }

    [Fact]
    public async Task Enrollments_need_permissions()
    {
        var student = await Data.StudentUserAsync(await Data.StudentAsync());

        (await Api.GetAsync("/api/enrollments", student.Token)).Expect(403);
        (await Api.PostAsync("/api/enrollments", new { }, student.Token)).Expect(403);
    }
}
