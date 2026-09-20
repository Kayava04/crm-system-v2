namespace Crm.IntegrationTests.Tests;

public class ReportingTests(CrmApiFactory factory) : ApiTest(factory)
{
    [Fact]
    public async Task Students_summary_counts_by_status()
    {
        var before = (await Api.GetAsync("/api/reports/students/summary", Admin)).Expect(200);
        var student = await Data.StudentAsync();
        await Data.StudentAsync();
        (await Api.PutAsync($"/api/students/{student}/status", new { status = "Graduated" }, Admin)).Expect(200);

        var after = (await Api.GetAsync("/api/reports/students/summary", Admin)).Expect(200);

        Assert.Equal(before["total"].GetValue<int>() + 2, after["total"].GetValue<int>());
        Assert.Equal(before["byStatus"]!["Graduated"]!.GetValue<int>() + 1, after["byStatus"]!["Graduated"]!.GetValue<int>());
        Assert.Equal(before["byStatus"]!["Active"]!.GetValue<int>() + 1, after["byStatus"]!["Active"]!.GetValue<int>());
        Assert.True(after["byStatus"]!.AsObject().ContainsKey("Withdrawn"));   // every status is present, even with zero
    }

    [Fact]
    public async Task Teachers_summary_has_a_salary_overview_of_working_teachers_only()
    {
        var before = (await Api.GetAsync("/api/reports/teachers/summary", Admin)).Expect(200);
        var working = await Data.TeacherAsync(baseSalary: 2000, lessonsRate: 200);
        var gone = await Data.TeacherAsync(baseSalary: 9000, lessonsRate: 900);
        (await Api.PutAsync($"/api/teachers/{gone}/status", new { status = "Dismissed" }, Admin)).Expect(200);

        var after = (await Api.GetAsync("/api/reports/teachers/summary", Admin)).Expect(200);

        Assert.Equal(before["total"].GetValue<int>() + 2, after["total"].GetValue<int>());
        Assert.Equal(before["salaryOverview"]!["teachersWithRate"]!.GetValue<int>() + 1, after["salaryOverview"]!["teachersWithRate"]!.GetValue<int>());
        Assert.Equal(before["salaryOverview"]!["totalBaseSalary"]!.GetValue<decimal>() + 2000m, after["salaryOverview"]!["totalBaseSalary"]!.GetValue<decimal>());
        Assert.NotEqual(working, gone);
    }

    [Fact]
    public async Task Enrollments_summary_counts_by_status_and_by_course_with_names()
    {
        var course = await Data.CourseAsync();
        await Data.EnrollmentAsync(await Data.StudentAsync(), course);
        await Data.EnrollmentAsync(await Data.StudentAsync(), course, activate: false);

        var summary = (await Api.GetAsync("/api/reports/enrollments/summary", Admin)).Expect(200);

        var byCourse = summary["byCourse"].AsArray().Single(c => c!["courseId"]!.GetValue<Guid>() == course)!;
        Assert.Equal(2, byCourse["count"]!.GetValue<int>());
        Assert.StartsWith("Course", byCourse["courseName"]!.GetValue<string>());
        Assert.True(summary["byStatus"]!["Active"]!.GetValue<int>() >= 1);
        Assert.True(summary["byStatus"]!["Draft"]!.GetValue<int>() >= 1);
    }

    [Fact]
    public async Task Billing_summary_adds_up_income_expenses_and_open_amounts_for_the_period()
    {
        var day = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var before = (await Api.GetAsync($"/api/reports/billing/summary?dateFrom={day}&dateTo={day}", Admin)).Expect(200);

        var enrollment = await Data.EnrollmentAsync(await Data.StudentAsync(), await Data.CourseAsync(price: 3000), discount: 2500);
        var paid = (await Api.PostAsync("/api/billing/invoices", new { enrollmentId = enrollment, period = "2032-01", dueDate = "2032-01-31", notes = (string?)null }, Admin)).Expect(201);
        (await Api.PutAsync($"/api/billing/invoices/{paid.Id}/paid", null, Admin)).Expect(204);
        (await Api.PostAsync("/api/billing/invoices", new { enrollmentId = enrollment, period = "2032-02", dueDate = "2032-02-28", notes = (string?)null }, Admin)).Expect(201);
        var payroll = (await Api.PostAsync("/api/billing/payrolls", new { teacherId = await Data.TeacherAsync(baseSalary: 1200, lessonsRate: 100), period = "2032-01" }, Admin)).Expect(201);
        (await Api.PutAsync($"/api/billing/payrolls/{payroll.Id}/paid", null, Admin)).Expect(204);

        var after = (await Api.GetAsync($"/api/reports/billing/summary?dateFrom={day}&dateTo={day}", Admin)).Expect(200);

        Assert.Equal(before["income"].GetValue<decimal>() + 2500m, after["income"].GetValue<decimal>());
        Assert.Equal(before["expenses"].GetValue<decimal>() + 1200m, after["expenses"].GetValue<decimal>());
        Assert.Equal(after["income"].GetValue<decimal>() - after["expenses"].GetValue<decimal>(), after["netResult"].GetValue<decimal>());
        Assert.Equal(before["pendingInvoicesAmount"].GetValue<decimal>() + 2500m, after["pendingInvoicesAmount"].GetValue<decimal>());
    }

    [Fact]
    public async Task Billing_summary_ignores_payments_outside_the_period()
    {
        var far = "2001-01-01";

        var summary = (await Api.GetAsync($"/api/reports/billing/summary?dateFrom={far}&dateTo={far}", Admin)).Expect(200);

        Assert.Equal(0m, summary["income"].GetValue<decimal>());
        Assert.Equal(0m, summary["expenses"].GetValue<decimal>());
        Assert.Equal(far, summary["dateFrom"].GetValue<string>());
    }

    [Fact]
    public async Task Billing_summary_validates_the_range_and_reports_are_admin_only()
    {
        (await Api.GetAsync("/api/reports/billing/summary?dateFrom=2030-02-01&dateTo=2030-01-01", Admin)).Expect(400);
        (await Api.GetAsync("/api/reports/billing/summary", Admin)).Expect(200);   // defaults to the current month

        var teacher = await Data.TeacherUserAsync(await Data.TeacherAsync());
        (await Api.GetAsync("/api/reports/students/summary", teacher.Token)).Expect(403);
        (await Api.GetAsync("/api/reports/teachers/summary")).Expect(401);
    }
}
