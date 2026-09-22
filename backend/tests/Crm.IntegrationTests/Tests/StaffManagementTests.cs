namespace Crm.IntegrationTests.Tests;

// The first administrator, created by the SuperAdmin, manages the other administrator and manager accounts
public class StaffManagementTests(CrmApiFactory factory) : ApiTest(factory)
{
    private async Task<TestUser> ManagerAsync(params string[] permissions) =>
        await Data.UserAsync("Admin", permissionIds: await Data.PermissionIdsAsync(permissions));

    private async Task<TestUser> AdminManagerAsync() => await ManagerAsync("CanManageAdmins");

    private async Task<Guid> SuperAdminIdAsync() => (await Api.GetAsync("/api/auth/me", Admin)).Expect(200)["userId"].GetValue<Guid>();

    // ------------------------------------------------------------------ who may
    [Fact]
    public async Task Only_a_holder_of_CanManageAdmins_can_manage_accounts()
    {
        var target = await Data.UserAsync("Admin");
        var plain = await ManagerAsync("CanViewReports");
        var student = await Data.StudentUserAsync(await Data.StudentAsync());
        var body = new { isActive = false };

        foreach (var token in new[] { plain.Token, student.Token })
        {
            (await Api.GetAsync("/api/auth/users", token)).Expect(403);
            (await Api.PutAsync($"/api/auth/users/{target.UserId}/status", body, token)).Expect(403);
            (await Api.PutAsync($"/api/auth/users/{target.UserId}/permissions", new { permissionIds = new List<Guid>() }, token)).Expect(403);
        }

        (await Api.GetAsync("/api/auth/users")).Expect(401);
        (await Api.PutAsync($"/api/auth/users/{target.UserId}/status", body)).Expect(401);
    }

    // ------------------------------------------------------------------ list
    [Fact]
    public async Task The_list_shows_administrators_with_details_and_leaves_out_the_super_admin_students_and_teachers()
    {
        var email = TestData.Email("manager");
        var registered = (await Api.PostAsync("/api/auth/register", new
        {
            email, role = "Admin", firstName = "Olena", lastName = "Kovalenko", phoneNumber = "+380501234567",
            permissionIds = await Data.PermissionIdsAsync("CanViewReports", "CanViewStudents")
        }, Admin)).Expect(201);
        var student = await Data.StudentUserAsync(await Data.StudentAsync());
        var teacher = await Data.TeacherUserAsync(await Data.TeacherAsync());

        var list = (await Api.GetAsync("/api/auth/users", Admin)).Expect(200).Json!.AsArray();

        var member = list.Single(m => m!["id"]!.GetValue<Guid>() == registered.Json!["userId"]!.GetValue<Guid>())!;
        Assert.Equal(email, member["email"]!.GetValue<string>());
        Assert.Equal("Olena Kovalenko", member["fullName"]!.GetValue<string>());
        Assert.Equal("+380501234567", member["phoneNumber"]!.GetValue<string>());
        Assert.True(member["isActive"]!.GetValue<bool>());
        Assert.Equal(["CanViewReports", "CanViewStudents"], member["permissions"]!.AsArray().Select(p => p!.GetValue<string>()));

        var ids = list.Select(m => m!["id"]!.GetValue<Guid>()).ToList();
        Assert.DoesNotContain(await SuperAdminIdAsync(), ids);
        Assert.DoesNotContain(student.UserId, ids);
        Assert.DoesNotContain(teacher.UserId, ids);
    }

    [Fact]
    public async Task The_list_can_be_limited_to_active_or_deactivated_accounts()
    {
        var active = await Data.UserAsync("Admin");
        var gone = await Data.UserAsync("Admin");
        (await Api.PutAsync($"/api/auth/users/{gone.UserId}/status", new { isActive = false }, Admin)).Expect(200);

        Guid[] Ids(ApiResponse r) => r.Json!.AsArray().Select(m => m!["id"]!.GetValue<Guid>()).ToArray();

        var onlyActive = Ids((await Api.GetAsync("/api/auth/users?isActive=true", Admin)).Expect(200));
        var onlyGone = Ids((await Api.GetAsync("/api/auth/users?isActive=false", Admin)).Expect(200));
        var all = Ids((await Api.GetAsync("/api/auth/users", Admin)).Expect(200));

        Assert.Contains(active.UserId, onlyActive);
        Assert.DoesNotContain(gone.UserId, onlyActive);
        Assert.Contains(gone.UserId, onlyGone);
        Assert.DoesNotContain(active.UserId, onlyGone);
        Assert.Contains(active.UserId, all);
        Assert.Contains(gone.UserId, all);
    }

    // ------------------------------------------------------------------ deactivate and reactivate
    [Fact]
    public async Task A_deactivated_administrator_cannot_log_in_or_refresh_and_keeps_everything_when_reactivated()
    {
        var manager = await AdminManagerAsync();
        var target = await Data.UserAsync("Admin", permissionIds: await Data.PermissionIdsAsync("CanViewReports"));
        (await Api.PutAsync("/api/auth/me/contact", new { firstName = "Ivan", lastName = "Petrenko" }, target.Token)).Expect(200);
        var refresh = (await Api.PostAsync("/api/auth/login", new { email = target.Email, password = target.Password })).Expect(200)["refreshToken"].GetValue<string>();

        var off = (await Api.PutAsync($"/api/auth/users/{target.UserId}/status", new { isActive = false }, manager.Token)).Expect(200);

        Assert.False(off["isActive"].GetValue<bool>());
        Assert.Equal(403, (await Api.PostAsync("/api/auth/login", new { email = target.Email, password = target.Password })).Code);
        Assert.Equal(401, (await Api.PostAsync("/api/auth/refresh", new { refreshToken = refresh })).Code);
        (await Api.GetAsync("/api/auth/me", target.Token)).Expect(401);

        var on = (await Api.PutAsync($"/api/auth/users/{target.UserId}/status", new { isActive = true }, manager.Token)).Expect(200);

        Assert.True(on["isActive"].GetValue<bool>());
        var token = await Data.LoginAsync(target.Email, target.Password);
        var me = (await Api.GetAsync("/api/auth/me", token)).Expect(200);
        Assert.Equal("Ivan Petrenko", me["contact"]!["fullName"]!.GetValue<string>());
        Assert.Equal(["CanViewReports"], me["permissions"].AsArray().Select(p => p!.GetValue<string>()));
        Assert.Equal("1", await Sql($"select count(*) from identity.users where \"Id\" = '{target.UserId}'"));   // nothing was ever deleted
    }

    // ------------------------------------------------------------------ permissions
    [Fact]
    public async Task Permissions_are_replaced_and_apply_to_the_next_login()
    {
        var manager = await AdminManagerAsync();
        var target = await Data.UserAsync("Admin", permissionIds: await Data.PermissionIdsAsync("CanViewReports"));

        var changed = (await Api.PutAsync($"/api/auth/users/{target.UserId}/permissions",
            new { permissionIds = await Data.PermissionIdsAsync("CanViewStudents", "CanViewTeachers", "CanViewStudents") }, manager.Token)).Expect(200);

        Assert.Equal(["CanViewStudents", "CanViewTeachers"], changed["permissions"].AsArray().Select(p => p!.GetValue<string>()));
        var me = (await Api.GetAsync("/api/auth/me", await Data.LoginAsync(target.Email, target.Password))).Expect(200);
        Assert.Equal(["CanViewStudents", "CanViewTeachers"], me["permissions"].AsArray().Select(p => p!.GetValue<string>()));
        Assert.Equal(200, (await Api.GetAsync("/api/students", await Data.LoginAsync(target.Email, target.Password))).Code);
        Assert.Equal(403, (await Api.GetAsync("/api/reports/students/summary", await Data.LoginAsync(target.Email, target.Password))).Code);

        var cleared = (await Api.PutAsync($"/api/auth/users/{target.UserId}/permissions", new { permissionIds = new List<Guid>() }, manager.Token)).Expect(200);
        Assert.Empty(cleared["permissions"].AsArray());
    }

    [Fact]
    public async Task Unknown_permissions_or_a_missing_list_are_refused_and_change_nothing()
    {
        var target = await Data.UserAsync("Admin", permissionIds: await Data.PermissionIdsAsync("CanViewReports"));

        (await Api.PutAsync($"/api/auth/users/{target.UserId}/permissions", new { permissionIds = new[] { Guid.NewGuid() } }, Admin)).Expect(400);
        (await Api.PutAsync($"/api/auth/users/{target.UserId}/permissions", new { }, Admin)).Expect(400);

        var me = (await Api.GetAsync("/api/auth/me", await Data.LoginAsync(target.Email, target.Password))).Expect(200);
        Assert.Equal(["CanViewReports"], me["permissions"].AsArray().Select(p => p!.GetValue<string>()));
    }

    // ------------------------------------------------------------------ salary
    [Fact]
    public async Task Salary_is_set_by_staff_management_only_never_by_the_account_itself()
    {
        var manager = await AdminManagerAsync();
        var target = await Data.UserAsync("Admin");

        Assert.Null((await Api.GetAsync("/api/auth/me", target.Token)).Expect(200)["contact"]!["salary"]);

        var withSalary = (await Api.PutAsync($"/api/auth/users/{target.UserId}/salary", new { salary = 42000.50 }, manager.Token)).Expect(200);
        Assert.Equal(42000.50m, withSalary["salary"].GetValue<decimal>());

        var list = (await Api.GetAsync("/api/auth/users", manager.Token)).Expect(200).Json!.AsArray();
        Assert.Equal(42000.50m, list.Single(m => m!["id"]!.GetValue<Guid>() == target.UserId)!["salary"]!.GetValue<decimal>());

        var me = (await Api.GetAsync("/api/auth/me", target.Token)).Expect(200);
        Assert.Equal(42000.50m, me["contact"]!["salary"]!.GetValue<decimal>());

        // the target cannot set it on themselves, only read it
        (await Api.PutAsync("/api/auth/me/contact", new { firstName = "X", lastName = "Y", salary = 999999 }, target.Token)).Expect(200);
        Assert.Equal(42000.50m, (await Api.GetAsync("/api/auth/me", target.Token)).Expect(200)["contact"]!["salary"]!.GetValue<decimal>());   // ignored, unchanged

        var cleared = (await Api.PutAsync($"/api/auth/users/{target.UserId}/salary", new { salary = (decimal?)null }, manager.Token)).Expect(200);
        Assert.Null(cleared.Json!["salary"]);
    }

    [Fact]
    public async Task A_negative_salary_is_refused()
    {
        var target = await Data.UserAsync("Admin");

        (await Api.PutAsync($"/api/auth/users/{target.UserId}/salary", new { salary = -1 }, Admin)).Expect(400);
    }

    // ------------------------------------------------------------------ what can never be done
    [Fact]
    public async Task The_super_admin_students_teachers_and_oneself_cannot_be_changed_here()
    {
        var manager = await AdminManagerAsync();
        var student = await Data.StudentUserAsync(await Data.StudentAsync());
        var teacher = await Data.TeacherUserAsync(await Data.TeacherAsync());
        var superAdmin = await SuperAdminIdAsync();
        var noPermissions = new { permissionIds = new List<Guid>() };

        foreach (var (id, code) in new[] { (superAdmin, 403), (student.UserId, 409), (teacher.UserId, 409), (manager.UserId, 409), (Guid.NewGuid(), 404) })
        {
            (await Api.PutAsync($"/api/auth/users/{id}/status", new { isActive = false }, manager.Token)).Expect(code);
            (await Api.PutAsync($"/api/auth/users/{id}/permissions", noPermissions, manager.Token)).Expect(code);
            (await Api.PutAsync($"/api/auth/users/{id}/salary", new { salary = 1000 }, manager.Token)).Expect(code);
        }

        // the SuperAdmin still works, the manager keeps the right to manage
        (await Api.GetAsync("/api/auth/me", Admin)).Expect(200);
        (await Api.GetAsync("/api/auth/users", manager.Token)).Expect(200);
    }

    [Fact]
    public async Task Nobody_can_become_a_super_admin_through_registration_or_permissions()
    {
        (await Api.PostAsync("/api/auth/register", new { email = TestData.Email(), role = "SuperAdmin" }, Admin)).Expect(400);

        var target = await Data.UserAsync("Admin");
        (await Api.PutAsync($"/api/auth/users/{target.UserId}/permissions",
            new { permissionIds = await Data.PermissionIdsAsync(Enum.GetNames<Identity.Contracts.Enums.SystemPermission>()) }, Admin)).Expect(200);

        var me = (await Api.GetAsync("/api/auth/me", await Data.LoginAsync(target.Email, target.Password))).Expect(200);
        Assert.Equal(["Admin"], me["roles"].AsArray().Select(r => r!.GetValue<string>()));   // all rights, but still not the SuperAdmin
    }
}
