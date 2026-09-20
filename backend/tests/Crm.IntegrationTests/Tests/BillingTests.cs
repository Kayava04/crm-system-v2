namespace Crm.IntegrationTests.Tests;

public class BillingTests(CrmApiFactory factory) : ApiTest(factory)
{
    private static string Period => DateTime.UtcNow.ToString("yyyy-MM");
    private static string InDays(int days) => DateTime.UtcNow.AddDays(days).ToString("yyyy-MM-dd");

    [Fact]
    public async Task An_invoice_takes_the_effective_price_of_the_enrollment()
    {
        var student = await Data.StudentAsync();
        var enrollment = await Data.EnrollmentAsync(student, await Data.CourseAsync(price: 3000), discount: 2500);

        var invoice = (await Api.PostAsync("/api/billing/invoices", new { enrollmentId = enrollment, period = Period, dueDate = InDays(10), notes = "n" }, Admin)).Expect(201);

        Assert.Equal(2500m, invoice["amount"].GetValue<decimal>());
        Assert.Equal(student, invoice["studentId"].GetValue<Guid>());
        Assert.Equal("Pending", invoice["status"].GetValue<string>());
    }

    [Fact]
    public async Task Invoice_rules_duplicates_periods_and_unknown_enrollments()
    {
        var enrollment = await Data.EnrollmentAsync(await Data.StudentAsync(), await Data.CourseAsync());
        object Body(Guid e, string p) => new { enrollmentId = e, period = p, dueDate = InDays(10), notes = (string?)null };

        (await Api.PostAsync("/api/billing/invoices", Body(enrollment, Period), Admin)).Expect(201);
        (await Api.PostAsync("/api/billing/invoices", Body(enrollment, Period), Admin)).Expect(409);          // one per enrollment and period
        (await Api.PostAsync("/api/billing/invoices", Body(enrollment, "2030-13"), Admin)).Expect(400);
        (await Api.PostAsync("/api/billing/invoices", Body(Guid.NewGuid(), Period), Admin)).Expect(404);
        (await Api.PostAsync("/api/billing/invoices", Body(enrollment, "2030-01"), Admin)).Expect(201);       // another period is fine
    }

    [Fact]
    public async Task An_invoice_is_paid_once()
    {
        var enrollment = await Data.EnrollmentAsync(await Data.StudentAsync(), await Data.CourseAsync());
        var invoice = (await Api.PostAsync("/api/billing/invoices", new { enrollmentId = enrollment, period = Period, dueDate = InDays(10), notes = (string?)null }, Admin)).Expect(201);

        (await Api.PutAsync($"/api/billing/invoices/{invoice.Id}/paid", null, Admin)).Expect(204);
        (await Api.PutAsync($"/api/billing/invoices/{invoice.Id}/paid", null, Admin)).Expect(409);

        var detail = (await Api.GetAsync($"/api/billing/invoices/{invoice.Id}", Admin)).Expect(200);
        Assert.Equal("Paid", detail["status"].GetValue<string>());
        Assert.NotNull(detail.Json!["paidAt"]);
        (await Api.PutAsync($"/api/billing/invoices/{Guid.NewGuid()}/paid", null, Admin)).Expect(404);
    }

    [Fact]
    public async Task Mark_overdue_touches_only_unpaid_invoices_past_their_due_date()
    {
        var enrollment = await Data.EnrollmentAsync(await Data.StudentAsync(), await Data.CourseAsync());
        object Body(string period, string due) => new { enrollmentId = enrollment, period, dueDate = due, notes = (string?)null };
        var late = (await Api.PostAsync("/api/billing/invoices", Body("2020-01", "2020-01-31"), Admin)).Expect(201);
        var paidLate = (await Api.PostAsync("/api/billing/invoices", Body("2020-02", "2020-02-28"), Admin)).Expect(201);
        var future = (await Api.PostAsync("/api/billing/invoices", Body("2020-03", InDays(30)), Admin)).Expect(201);
        (await Api.PutAsync($"/api/billing/invoices/{paidLate.Id}/paid", null, Admin)).Expect(204);

        var marked = (await Api.PutAsync("/api/billing/invoices/mark-overdue", null, Admin)).Expect(200);

        Assert.True(marked["updatedCount"].GetValue<int>() >= 1);
        Assert.Equal("Overdue", (await Api.GetAsync($"/api/billing/invoices/{late.Id}", Admin))["status"].GetValue<string>());
        Assert.Equal("Paid", (await Api.GetAsync($"/api/billing/invoices/{paidLate.Id}", Admin))["status"].GetValue<string>());
        Assert.Equal("Pending", (await Api.GetAsync($"/api/billing/invoices/{future.Id}", Admin))["status"].GetValue<string>());
    }

    [Fact]
    public async Task Invoices_can_be_filtered_by_student_and_status()
    {
        var student = await Data.StudentAsync();
        var enrollment = await Data.EnrollmentAsync(student, await Data.CourseAsync());
        await Api.PostAsync("/api/billing/invoices", new { enrollmentId = enrollment, period = "2031-01", dueDate = InDays(10), notes = (string?)null }, Admin);
        var second = (await Api.PostAsync("/api/billing/invoices", new { enrollmentId = enrollment, period = "2031-02", dueDate = InDays(10), notes = (string?)null }, Admin)).Expect(201);
        (await Api.PutAsync($"/api/billing/invoices/{second.Id}/paid", null, Admin)).Expect(204);

        Assert.Equal(2, (await Api.GetAsync($"/api/billing/invoices?studentId={student}", Admin))["totalCount"].GetValue<int>());
        Assert.Equal(1, (await Api.GetAsync($"/api/billing/invoices?studentId={student}&status=Paid", Admin))["totalCount"].GetValue<int>());
        Assert.Equal(1, (await Api.GetAsync($"/api/billing/invoices?studentId={student}&period=2031-01", Admin))["totalCount"].GetValue<int>());
    }

    [Fact]
    public async Task Payroll_is_the_base_salary_plus_the_rate_for_each_completed_lesson_of_the_month()
    {
        var teacher = await Data.TeacherAsync(baseSalary: 1000, lessonsRate: 100);
        var enrollment = await Data.EnrollmentAsync(await Data.StudentAsync(), await Data.CourseAsync());
        (await Data.GenerateAsync(enrollmentId: enrollment, teacherId: teacher)).Expect(201);
        var lessons = await Data.LessonsAsync(enrollmentId: enrollment);
        var month = lessons[0]!["scheduledDate"]!.GetValue<DateTime>().ToString("yyyy-MM");
        var completedInMonth = 0;

        foreach (var lesson in lessons.Take(3))
        {
            (await Api.PutAsync($"/api/schedules/{lesson!["id"]}/complete", null, Admin)).Expect(204);
            if (lesson["scheduledDate"]!.GetValue<DateTime>().ToString("yyyy-MM") == month) completedInMonth++;
        }

        var payroll = (await Api.PostAsync("/api/billing/payrolls", new { teacherId = teacher, period = month }, Admin)).Expect(201);

        Assert.Equal(completedInMonth, payroll["completedLessonsCount"].GetValue<int>());
        Assert.Equal(1000m + 100m * completedInMonth, payroll["totalAmount"].GetValue<decimal>());
        Assert.Equal(1000m, payroll["baseSalary"].GetValue<decimal>());
        Assert.Equal(100m, payroll["lessonsRate"].GetValue<decimal>());
    }

    [Fact]
    public async Task Payroll_rules_duplicates_unknown_teachers_and_paying()
    {
        var teacher = await Data.TeacherAsync();
        object Body(Guid t, string p) => new { teacherId = t, period = p };

        var payroll = (await Api.PostAsync("/api/billing/payrolls", Body(teacher, "2031-05"), Admin)).Expect(201);
        Assert.Equal(1000m, payroll["totalAmount"].GetValue<decimal>());   // no lessons: base salary only

        (await Api.PostAsync("/api/billing/payrolls", Body(teacher, "2031-05"), Admin)).Expect(409);
        (await Api.PostAsync("/api/billing/payrolls", Body(Guid.NewGuid(), "2031-05"), Admin)).Expect(404);
        (await Api.PostAsync("/api/billing/payrolls", Body(teacher, "bad"), Admin)).Expect(400);

        (await Api.PutAsync($"/api/billing/payrolls/{payroll.Id}/paid", null, Admin)).Expect(204);
        (await Api.PutAsync($"/api/billing/payrolls/{payroll.Id}/paid", null, Admin)).Expect(409);
        Assert.Equal("Paid", (await Api.GetAsync($"/api/billing/payrolls/{payroll.Id}", Admin))["status"].GetValue<string>());
        Assert.Equal(1, (await Api.GetAsync($"/api/billing/payrolls?teacherId={teacher}&status=Paid", Admin))["totalCount"].GetValue<int>());
    }

    [Fact]
    public async Task Payroll_keeps_a_snapshot_of_the_rate_it_was_calculated_with()
    {
        var teacher = await Data.TeacherAsync(baseSalary: 1000, lessonsRate: 100);
        var payroll = (await Api.PostAsync("/api/billing/payrolls", new { teacherId = teacher, period = "2031-06" }, Admin)).Expect(201);

        (await Api.PostAsync($"/api/teachers/{teacher}/salary-rates", new { baseSalary = 5000, lessonsRate = 500, effectiveFrom = DateTime.UtcNow.AddDays(1) }, Admin)).Expect(204);
        await Factory.Database.ExecuteAsync($@"update teachers.teacher_salary_rates set ""EffectiveFrom"" = ""EffectiveFrom"" - interval '2 days' where ""TeacherId"" = '{teacher}'");
        Assert.Equal(5000m, (await Api.GetAsync($"/api/teachers/{teacher}", Admin))["currentSalaryRate"]!["baseSalary"]!.GetValue<decimal>());   // the new rate is in force now

        var detail = (await Api.GetAsync($"/api/billing/payrolls/{payroll.Id}", Admin)).Expect(200);
        Assert.Equal(1000m, detail["baseSalary"].GetValue<decimal>());
    }

    [Fact]
    public async Task Billing_is_closed_to_students_and_teachers()
    {
        var student = await Data.StudentUserAsync(await Data.StudentAsync());
        var teacher = await Data.TeacherUserAsync(await Data.TeacherAsync());

        (await Api.GetAsync("/api/billing/invoices", student.Token)).Expect(403);
        (await Api.GetAsync("/api/billing/payrolls", teacher.Token)).Expect(403);
        (await Api.PostAsync("/api/billing/payrolls", new { }, teacher.Token)).Expect(403);
    }
}
