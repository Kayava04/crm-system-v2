namespace Crm.IntegrationTests.Tests;

public class AuthTests(CrmApiFactory factory) : ApiTest(factory)
{
    [Fact]
    public async Task Login_with_wrong_password_is_rejected_and_unknown_email_looks_the_same()
    {
        var wrongPassword = (await Api.PostAsync("/api/auth/login", new { email = CrmApiFactory.AdminEmail, password = "nope" })).Expect(401);
        var unknownEmail = (await Api.PostAsync("/api/auth/login", new { email = "nobody@example.test", password = "nope" })).Expect(401);

        Assert.Equal(wrongPassword["detail"].GetValue<string>(), unknownEmail["detail"].GetValue<string>());
    }

    [Fact]
    public async Task SuperAdmin_gets_tokens_and_does_not_have_to_change_the_password()
    {
        var login = (await Api.PostAsync("/api/auth/login", new { email = CrmApiFactory.AdminEmail, password = CrmApiFactory.AdminPassword })).Expect(200);

        Assert.False(login["mustChangePassword"].GetValue<bool>());
        Assert.False(string.IsNullOrEmpty(login["accessToken"].GetValue<string>()));
        Assert.False(string.IsNullOrEmpty(login["refreshToken"].GetValue<string>()));
    }

    [Fact]
    public async Task Register_is_closed_to_anonymous_and_ordinary_users()
    {
        var student = await Data.StudentUserAsync(await Data.StudentAsync());

        (await Api.PostAsync("/api/auth/register", new { email = TestData.Email(), role = "Student" })).Expect(401);
        (await Api.PostAsync("/api/auth/register", new { email = TestData.Email(), role = "Student" }, student.Token)).Expect(403);
    }

    [Fact]
    public async Task Register_returns_a_temporary_password_that_must_be_changed()
    {
        var user = await Data.UserAsync("Admin");

        var login = (await Api.PostAsync("/api/auth/login", new { email = user.Email, password = user.Password })).Expect(200);

        Assert.True(login["mustChangePassword"].GetValue<bool>());
        Assert.Equal(12, user.Password.Length);
    }

    [Fact]
    public async Task Register_rejects_a_duplicate_email_and_bad_input()
    {
        var user = await Data.UserAsync("Admin");

        (await Api.PostAsync("/api/auth/register", new { email = user.Email, role = "Admin" }, Admin)).Expect(409);
        (await Api.PostAsync("/api/auth/register", new { email = "not-an-email", role = "Admin" }, Admin)).Expect(400);
        (await Api.PostAsync("/api/auth/register", new { email = TestData.Email(), role = "SuperAdmin" }, Admin)).Expect(400);
    }

    [Fact]
    public async Task Register_links_the_profile_and_HasAccount_becomes_true()
    {
        var studentId = await Data.StudentAsync();
        Assert.False((await Api.GetAsync($"/api/students/{studentId}", Admin)).Expect(200)["hasAccount"].GetValue<bool>());

        await Data.StudentUserAsync(studentId);

        Assert.True((await Api.GetAsync($"/api/students/{studentId}", Admin)).Expect(200)["hasAccount"].GetValue<bool>());
    }

    [Fact]
    public async Task Register_for_a_missing_profile_creates_no_account()
    {
        var email = TestData.Email();

        (await Api.PostAsync("/api/auth/register", new { email, role = "Student", profileType = "Student", profileId = Guid.NewGuid() }, Admin)).Expect(404);

        // nothing was left behind: the same email can be registered afterwards
        (await Api.PostAsync("/api/auth/register", new { email, role = "Student" }, Admin)).Expect(201);
    }

    [Fact]
    public async Task Register_for_an_unknown_profile_type_creates_no_account()
    {
        var email = TestData.Email();

        (await Api.PostAsync("/api/auth/register", new { email, role = "Student", profileType = "Alien", profileId = Guid.NewGuid() }, Admin)).Expect(409);

        (await Api.PostAsync("/api/auth/register", new { email, role = "Student" }, Admin)).Expect(201);
    }

    [Fact]
    public async Task A_profile_can_have_only_one_account()
    {
        var studentId = await Data.StudentAsync();
        await Data.StudentUserAsync(studentId);

        var second = TestData.Email();
        (await Api.PostAsync("/api/auth/register", new { email = second, role = "Student", profileType = "Student", profileId = studentId }, Admin)).Expect(409);

        // the failed attempt did not leave an orphan account behind
        (await Api.PostAsync("/api/auth/register", new { email = second, role = "Student" }, Admin)).Expect(201);
    }

    [Fact]
    public async Task Registering_a_user_creates_a_change_password_notification_that_changing_the_password_resolves()
    {
        var user = await Data.UserAsync("Student");

        var unread = (await Api.GetAsync("/api/notifications/unread-count", user.Token)).Expect(200);
        Assert.Equal(1, unread["count"].GetValue<int>());

        var notification = (await Api.GetAsync("/api/notifications?unreadOnly=true", user.Token)).Expect(200).Items.Single()!;
        Assert.Equal("PasswordChangeRequired", notification["type"]!.GetValue<string>());
        Assert.Equal("ChangePassword", notification["action"]!.GetValue<string>());

        (await Api.PostAsync("/api/auth/change-password", new
        {
            oldPassword = user.Password,
            newPassword = "NewPass123!",
            confirmNewPassword = "NewPass123!"
        }, user.Token)).Expect(204);

        Assert.Equal(0, (await Api.GetAsync("/api/notifications/unread-count", user.Token)).Expect(200)["count"].GetValue<int>());

        var login = (await Api.PostAsync("/api/auth/login", new { email = user.Email, password = "NewPass123!" })).Expect(200);
        Assert.False(login["mustChangePassword"].GetValue<bool>());
    }

    [Fact]
    public async Task Change_password_validates_the_old_password_and_the_new_one()
    {
        var user = await Data.UserAsync("Student");

        (await Api.PostAsync("/api/auth/change-password", new { oldPassword = "wrong", newPassword = "NewPass123!", confirmNewPassword = "NewPass123!" }, user.Token)).Expect(400);
        (await Api.PostAsync("/api/auth/change-password", new { oldPassword = user.Password, newPassword = "short", confirmNewPassword = "short" }, user.Token)).Expect(400);
        (await Api.PostAsync("/api/auth/change-password", new { oldPassword = user.Password, newPassword = "NewPass123!", confirmNewPassword = "Different1!" }, user.Token)).Expect(400);
        (await Api.PostAsync("/api/auth/change-password", new { oldPassword = user.Password, newPassword = "NewPass123!", confirmNewPassword = "NewPass123!" })).Expect(401);
    }

    [Fact]
    public async Task Admin_reset_gives_a_new_temporary_password_and_revokes_the_old_sessions()
    {
        var user = await Data.UserAsync("Student");
        var login = (await Api.PostAsync("/api/auth/login", new { email = user.Email, password = user.Password })).Expect(200);
        var refresh = login["refreshToken"].GetValue<string>();

        var reset = (await Api.PostAsync($"/api/auth/users/{user.UserId}/reset-password", null, Admin)).Expect(200);
        var newPassword = reset["temporaryPassword"].GetValue<string>();

        (await Api.PostAsync("/api/auth/login", new { email = user.Email, password = user.Password })).Expect(401);
        (await Api.PostAsync("/api/auth/refresh", new { refreshToken = refresh })).Expect(401);

        var relogin = (await Api.PostAsync("/api/auth/login", new { email = user.Email, password = newPassword })).Expect(200);
        Assert.True(relogin["mustChangePassword"].GetValue<bool>());
        Assert.True((await Api.GetAsync("/api/notifications/unread-count", relogin["accessToken"].GetValue<string>())).Expect(200)["count"].GetValue<int>() >= 1);
    }

    [Fact]
    public async Task Reset_password_is_protected()
    {
        var user = await Data.UserAsync("Student");

        (await Api.PostAsync($"/api/auth/users/{user.UserId}/reset-password", null)).Expect(401);
        (await Api.PostAsync($"/api/auth/users/{user.UserId}/reset-password", null, user.Token)).Expect(403);
        (await Api.PostAsync($"/api/auth/users/{Guid.NewGuid()}/reset-password", null, Admin)).Expect(404);
    }

    [Fact]
    public async Task The_SuperAdmin_password_cannot_be_reset()
    {
        var superAdminId = await Sql("select \"Id\" from identity.users where \"Email\" = '" + CrmApiFactory.AdminEmail + "'");

        (await Api.PostAsync($"/api/auth/users/{superAdminId}/reset-password", null, Admin)).Expect(403);
        (await Api.PostAsync("/api/auth/login", new { email = CrmApiFactory.AdminEmail, password = CrmApiFactory.AdminPassword })).Expect(200);
    }

    [Fact]
    public async Task Refresh_rotates_the_token_and_a_used_token_stops_working()
    {
        var user = await Data.UserAsync("Student");
        var login = (await Api.PostAsync("/api/auth/login", new { email = user.Email, password = user.Password })).Expect(200);
        var first = login["refreshToken"].GetValue<string>();

        var refreshed = (await Api.PostAsync("/api/auth/refresh", new { refreshToken = first })).Expect(200);
        var second = refreshed["refreshToken"].GetValue<string>();

        Assert.NotEqual(first, second);
        Assert.False(string.IsNullOrEmpty(refreshed["accessToken"].GetValue<string>()));
        (await Api.PostAsync("/api/auth/refresh", new { refreshToken = first })).Expect(401);
        (await Api.PostAsync("/api/auth/refresh", new { refreshToken = second })).Expect(200);
        (await Api.PostAsync("/api/auth/refresh", new { refreshToken = "garbage" })).Expect(401);
    }

    [Fact]
    public async Task Default_permissions_reach_teachers_and_students_through_their_tokens()
    {
        var teacher = await Data.TeacherUserAsync(await Data.TeacherAsync());
        var student = await Data.StudentUserAsync(await Data.StudentAsync());
        var course = await Data.CourseAsync();
        var material = new { courseId = course, type = "Link", title = "t", description = (string?)null, body = (string?)null, url = "https://example.com" };

        (await Api.PostAsync("/api/materials", material, teacher.Token)).Expect(201);
        (await Api.PostAsync("/api/materials", material, student.Token)).Expect(403);
        (await Api.GetAsync("/api/materials", student.Token)).Expect(200);
        (await Api.GetAsync("/api/students", student.Token)).Expect(403);
    }
}
