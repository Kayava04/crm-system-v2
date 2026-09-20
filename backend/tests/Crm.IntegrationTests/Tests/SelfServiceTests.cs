namespace Crm.IntegrationTests.Tests;

// What a student or a teacher can see about themselves without any administrator permission
public class SelfServiceTests(CrmApiFactory factory) : ApiTest(factory)
{
    private static string Period => DateTime.UtcNow.ToString("yyyy-MM");

    // ------------------------------------------------------------------ who am I
    [Fact]
    public async Task Me_describes_a_student_with_the_linked_profile()
    {
        var studentId = await Data.StudentAsync();
        var user = await Data.StudentUserAsync(studentId);

        var me = (await Api.GetAsync("/api/auth/me", user.Token)).Expect(200);

        Assert.Equal(user.UserId, me["userId"].GetValue<Guid>());
        Assert.Equal(user.Email, me["email"].GetValue<string>());
        Assert.True(me["mustChangePassword"].GetValue<bool>());
        Assert.Equal(["Student"], me["roles"].AsArray().Select(r => r!.GetValue<string>()));
        Assert.Equal(["CanViewMaterials"], me["permissions"].AsArray().Select(r => r!.GetValue<string>()));
        Assert.Equal("Student", me["profile"]!["type"]!.GetValue<string>());
        Assert.Equal(studentId, me["profile"]!["id"]!.GetValue<Guid>());
        Assert.Contains("Student", me["profile"]!["fullName"]!.GetValue<string>());
    }

    [Fact]
    public async Task Me_describes_a_teacher_and_an_administrator()
    {
        var teacherId = await Data.TeacherAsync();
        var teacher = await Data.TeacherUserAsync(teacherId);
        var admin = await Data.UserAsync("Admin", permissionIds: await Data.PermissionIdsAsync("CanViewReports", "CanViewStudents"));

        var teacherMe = (await Api.GetAsync("/api/auth/me", teacher.Token)).Expect(200);
        Assert.Equal("Teacher", teacherMe["profile"]!["type"]!.GetValue<string>());
        Assert.Equal(teacherId, teacherMe["profile"]!["id"]!.GetValue<Guid>());
        Assert.Equal(["CanManageMaterials", "CanViewMaterials"], teacherMe["permissions"].AsArray().Select(p => p!.GetValue<string>()));

        var adminMe = (await Api.GetAsync("/api/auth/me", admin.Token)).Expect(200);
        Assert.Null(adminMe.Json!["profile"]);
        Assert.Equal(["Admin"], adminMe["roles"].AsArray().Select(r => r!.GetValue<string>()));
        Assert.Equal(["CanViewReports", "CanViewStudents"], adminMe["permissions"].AsArray().Select(p => p!.GetValue<string>()));

        var superAdmin = (await Api.GetAsync("/api/auth/me", Admin)).Expect(200);
        Assert.Contains("SuperAdmin", superAdmin["roles"].AsArray().Select(r => r!.GetValue<string>()));
        Assert.Equal(Enum.GetNames<Identity.Contracts.Enums.SystemPermission>().Order(), superAdmin["permissions"].AsArray().Select(p => p!.GetValue<string>()).Order());
    }

    [Fact]
    public async Task Me_is_refused_for_anonymous_and_deactivated_accounts()
    {
        (await Api.GetAsync("/api/auth/me")).Expect(401);

        var studentId = await Data.StudentAsync();
        var user = await Data.StudentUserAsync(studentId);
        (await Api.PutAsync($"/api/students/{studentId}/status", new { status = "Withdrawn" }, Admin)).Expect(200);

        // the access token is still valid for a few minutes, but the account is not
        (await Api.GetAsync("/api/auth/me", user.Token)).Expect(401);
    }

    [Fact]
    public async Task Me_answers_for_an_account_without_a_profile()
    {
        var user = await Data.UserAsync("Student");

        var me = (await Api.GetAsync("/api/auth/me", user.Token)).Expect(200);

        Assert.Null(me.Json!["profile"]);
    }

    // ------------------------------------------------------------------ contact details of administrators
    [Fact]
    public async Task An_administrator_registered_with_contact_details_sees_them_in_me()
    {
        var email = TestData.Email("manager");
        var registered = (await Api.PostAsync("/api/auth/register", new
        {
            email, role = "Admin", firstName = " Olena ", lastName = "Kovalenko", phoneNumber = "+380501234567"
        }, Admin)).Expect(201);
        var token = await Data.LoginAsync(email, registered["temporaryPassword"].GetValue<string>());

        var me = (await Api.GetAsync("/api/auth/me", token)).Expect(200);

        Assert.Null(me.Json!["profile"]);
        Assert.Equal("Olena", me["contact"]!["firstName"]!.GetValue<string>());
        Assert.Equal("Olena Kovalenko", me["contact"]!["fullName"]!.GetValue<string>());
        Assert.Equal("+380501234567", me["contact"]!["phoneNumber"]!.GetValue<string>());
        Assert.Equal(["Admin"], me["roles"].AsArray().Select(r => r!.GetValue<string>()));
    }

    [Fact]
    public async Task An_administrator_fills_in_and_changes_their_own_contact_details()
    {
        var admin = await Data.UserAsync("Admin");

        var empty = (await Api.GetAsync("/api/auth/me", admin.Token)).Expect(200);
        Assert.Null(empty["contact"]!["fullName"]);

        (await Api.PutAsync("/api/auth/me/contact", new { firstName = "Ivan", lastName = "Petrenko", phoneNumber = "+38 (050) 111-22-33" }, admin.Token)).Expect(200);
        (await Api.PutAsync("/api/auth/me/contact", new { firstName = "Ivan", lastName = "Melnyk" }, admin.Token)).Expect(200);   // the phone is optional and is cleared

        var me = (await Api.GetAsync("/api/auth/me", admin.Token)).Expect(200);
        Assert.Equal("Ivan Melnyk", me["contact"]!["fullName"]!.GetValue<string>());
        Assert.Null(me["contact"]!["phoneNumber"]);

        // the SuperAdmin is the system account, not a member of the staff: it has no personal details
        (await Api.PutAsync("/api/auth/me/contact", new { firstName = "Root", lastName = "Admin" }, Admin)).Expect(403);
        Assert.Null((await Api.GetAsync("/api/auth/me", Admin)).Expect(200)["contact"]!["fullName"]);
    }

    [Fact]
    public async Task Contact_details_are_validated_and_belong_to_accounts_without_a_student_or_teacher_record()
    {
        var admin = await Data.UserAsync("Admin");
        var student = await Data.StudentUserAsync(await Data.StudentAsync());
        var teacher = await Data.TeacherUserAsync(await Data.TeacherAsync());

        (await Api.PutAsync("/api/auth/me/contact", new { firstName = "", lastName = "X" }, admin.Token)).Expect(400);
        (await Api.PutAsync("/api/auth/me/contact", new { firstName = "X", lastName = new string('a', 101) }, admin.Token)).Expect(400);
        (await Api.PutAsync("/api/auth/me/contact", new { firstName = "X", lastName = "Y", phoneNumber = "call me" }, admin.Token)).Expect(400);
        (await Api.PostAsync("/api/auth/register", new { email = TestData.Email(), role = "Admin", phoneNumber = "abc" }, Admin)).Expect(400);

        // students and teachers keep their details in their own record: no second copy
        (await Api.PutAsync("/api/auth/me/contact", new { firstName = "A", lastName = "B" }, student.Token)).Expect(409);
        (await Api.PutAsync("/api/auth/me/contact", new { firstName = "A", lastName = "B" }, teacher.Token)).Expect(409);
        (await Api.PutAsync("/api/auth/me/contact", new { firstName = "A", lastName = "B" })).Expect(401);
    }

    // ------------------------------------------------------------------ own profile
    [Fact]
    public async Task A_student_reads_their_own_profile_and_only_a_student_can()
    {
        var studentId = await Data.StudentAsync();
        var user = await Data.StudentUserAsync(studentId);
        var teacher = await Data.TeacherUserAsync(await Data.TeacherAsync());

        var profile = (await Api.GetAsync("/api/students/me", user.Token)).Expect(200);

        Assert.Equal(studentId, profile["id"].GetValue<Guid>());
        Assert.True(profile["hasAccount"].GetValue<bool>());
        Assert.Equal("Work", profile["preferences"]!["learningGoal"]!.GetValue<string>());
        Assert.Equal("English", profile["languages"]![0]!.GetValue<string>());

        (await Api.GetAsync("/api/students/me", teacher.Token)).Expect(403);
        (await Api.GetAsync("/api/students/me", Admin)).Expect(403);
        (await Api.GetAsync("/api/students/me")).Expect(401);
        (await Api.GetAsync("/api/students/me", (await Data.UserAsync("Student")).Token)).Expect(404);   // no profile linked
    }

    [Fact]
    public async Task A_teacher_reads_their_own_profile_with_salary_rates()
    {
        var teacherId = await Data.TeacherAsync(baseSalary: 1700, lessonsRate: 170);
        var user = await Data.TeacherUserAsync(teacherId);
        var student = await Data.StudentUserAsync(await Data.StudentAsync());

        var profile = (await Api.GetAsync("/api/teachers/me", user.Token)).Expect(200);

        Assert.Equal(teacherId, profile["id"].GetValue<Guid>());
        Assert.Equal(1700m, profile["currentSalaryRate"]!["baseSalary"]!.GetValue<decimal>());
        Assert.Single(profile["salaryRates"].AsArray());

        (await Api.GetAsync("/api/teachers/me", student.Token)).Expect(403);
        (await Api.GetAsync("/api/teachers/me/students", user.Token)).Expect(200);   // the older route next to it still works
    }

    // ------------------------------------------------------------------ own enrollments
    [Fact]
    public async Task A_student_sees_only_their_own_enrollments_with_course_names_newest_first()
    {
        var studentId = await Data.StudentAsync();
        var user = await Data.StudentUserAsync(studentId);
        var course1 = await Data.CourseAsync();
        var course2 = await Data.CourseAsync(price: 4000);
        var older = (await Api.PostAsync("/api/enrollments", new { studentId, courseId = course1, startDate = "2031-01-10", discountedPrice = (decimal?)null, comment = (string?)null, preferredSchedule = (string?)null }, Admin)).Expect(201).Id;
        var newer = (await Api.PostAsync("/api/enrollments", new { studentId, courseId = course2, startDate = "2031-05-10", discountedPrice = 3500, comment = (string?)null, preferredSchedule = (string?)null }, Admin)).Expect(201).Id;
        await Data.EnrollmentAsync(await Data.StudentAsync(), course1);   // somebody else's

        var mine = (await Api.GetAsync("/api/enrollments/my", user.Token)).Expect(200).Json!.AsArray();

        Assert.Equal([newer, older], mine.Select(e => e!["id"]!.GetValue<Guid>()));
        Assert.StartsWith("Course", mine[0]!["courseName"]!.GetValue<string>());
        Assert.Equal(3500m, mine[0]!["effectivePrice"]!.GetValue<decimal>());
        Assert.Equal("Draft", mine[0]!["status"]!.GetValue<string>());
        Assert.StartsWith("CTR-20310510-", mine[0]!["enrollmentNumber"]!.GetValue<string>());
    }

    [Fact]
    public async Task Own_enrollments_are_closed_to_others()
    {
        var teacher = await Data.TeacherUserAsync(await Data.TeacherAsync());

        (await Api.GetAsync("/api/enrollments/my", teacher.Token)).Expect(403);
        (await Api.GetAsync("/api/enrollments/my", Admin)).Expect(403);
        (await Api.GetAsync("/api/enrollments/my")).Expect(401);
        (await Api.GetAsync("/api/enrollments/my", (await Data.UserAsync("Student")).Token)).Expect(404);
        Assert.Empty((await Api.GetAsync("/api/enrollments/my", (await Data.StudentUserAsync(await Data.StudentAsync())).Token)).Expect(200).Json!.AsArray());
    }

    // ------------------------------------------------------------------ own invoices and payrolls
    [Fact]
    public async Task A_student_sees_only_their_own_invoices_and_can_filter_them()
    {
        var studentId = await Data.StudentAsync();
        var user = await Data.StudentUserAsync(studentId);
        var enrollment = await Data.EnrollmentAsync(studentId, await Data.CourseAsync(price: 3000), discount: 2500);
        var stranger = await Data.EnrollmentAsync(await Data.StudentAsync(), await Data.CourseAsync());
        object Body(Guid e, string period) => new { enrollmentId = e, period, dueDate = "2035-12-01", notes = (string?)null };
        var first = (await Api.PostAsync("/api/billing/invoices", Body(enrollment, "2035-01"), Admin)).Expect(201);
        await Api.PostAsync("/api/billing/invoices", Body(enrollment, "2035-02"), Admin);
        await Api.PostAsync("/api/billing/invoices", Body(stranger, "2035-01"), Admin);
        (await Api.PutAsync($"/api/billing/invoices/{first.Id}/paid", null, Admin)).Expect(204);

        var all = (await Api.GetAsync("/api/billing/invoices/my", user.Token)).Expect(200);
        var paid = (await Api.GetAsync("/api/billing/invoices/my?status=Paid", user.Token)).Expect(200);
        var paged = (await Api.GetAsync("/api/billing/invoices/my?pageSize=1&page=2", user.Token)).Expect(200);

        Assert.Equal(2, all["totalCount"].GetValue<int>());
        Assert.All(all.Items, i => Assert.Equal(studentId, i!["studentId"]!.GetValue<Guid>()));
        Assert.Equal(2500m, all.Items[0]!["amount"]!.GetValue<decimal>());
        Assert.Equal(first.Id, Assert.Single(paid.Items)!["id"]!.GetValue<Guid>());
        Assert.Equal(2, paged["totalPages"].GetValue<int>());
    }

    [Fact]
    public async Task A_teacher_sees_only_their_own_payrolls()
    {
        var teacherId = await Data.TeacherAsync(baseSalary: 1500, lessonsRate: 100);
        var user = await Data.TeacherUserAsync(teacherId);
        var otherTeacher = await Data.TeacherAsync();
        var mine = (await Api.PostAsync("/api/billing/payrolls", new { teacherId, period = "2035-03" }, Admin)).Expect(201);
        await Api.PostAsync("/api/billing/payrolls", new { teacherId = otherTeacher, period = "2035-03" }, Admin);
        (await Api.PutAsync($"/api/billing/payrolls/{mine.Id}/paid", null, Admin)).Expect(204);

        var payrolls = (await Api.GetAsync("/api/billing/payrolls/my", user.Token)).Expect(200);

        Assert.Equal(1, payrolls["totalCount"].GetValue<int>());
        Assert.Equal(mine.Id, payrolls.Items[0]!["id"]!.GetValue<Guid>());
        Assert.Equal(1500m, payrolls.Items[0]!["totalAmount"]!.GetValue<decimal>());
        Assert.Equal(1, (await Api.GetAsync("/api/billing/payrolls/my?status=Paid", user.Token))["totalCount"].GetValue<int>());
        Assert.Equal(0, (await Api.GetAsync("/api/billing/payrolls/my?period=2000-01", user.Token))["totalCount"].GetValue<int>());
    }

    [Fact]
    public async Task Own_finances_are_closed_to_the_wrong_role()
    {
        var student = await Data.StudentUserAsync(await Data.StudentAsync());
        var teacher = await Data.TeacherUserAsync(await Data.TeacherAsync());

        (await Api.GetAsync("/api/billing/invoices/my", teacher.Token)).Expect(403);
        (await Api.GetAsync("/api/billing/payrolls/my", student.Token)).Expect(403);
        (await Api.GetAsync("/api/billing/invoices/my", Admin)).Expect(403);
        (await Api.GetAsync("/api/billing/payrolls/my", Admin)).Expect(403);
        (await Api.GetAsync("/api/billing/invoices/my")).Expect(401);
        (await Api.GetAsync("/api/billing/invoices/my", (await Data.UserAsync("Student")).Token)).Expect(404);
        (await Api.GetAsync("/api/billing/payrolls/my", (await Data.UserAsync("Teacher")).Token)).Expect(404);
    }
}
