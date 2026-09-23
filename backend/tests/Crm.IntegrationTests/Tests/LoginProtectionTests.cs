using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Crm.IntegrationTests.Tests;

// Guessing passwords must be slow: an account locks after repeated failures and login is rate limited per address
public class LoginProtectionTests(CrmApiFactory factory) : ApiTest(factory)
{
    private Task<ApiResponse> Login(string email, string password, Api? api = null) =>
        (api ?? Api).PostAsync("/api/auth/login", new { email, password });

    // ------------------------------------------------------------------ account lockout
    [Fact]
    public async Task Five_wrong_passwords_lock_the_account_even_for_the_right_password()
    {
        var user = await Data.UserAsync("Student");

        for (var i = 0; i < 5; i++)
            (await Login(user.Email, "wrong-password")).Expect(401);

        var locked = (await Login(user.Email, user.Password)).Expect(429);   // the right password is refused too
        Assert.Contains("locked", locked["detail"].GetValue<string>());
        Assert.Contains("minute", locked["detail"].GetValue<string>());
        (await Login(user.Email, "wrong-password")).Expect(429);
    }

    [Fact]
    public async Task Locking_one_account_does_not_affect_others_and_unknown_emails_look_like_wrong_passwords()
    {
        var victim = await Data.UserAsync("Student");
        var other = await Data.UserAsync("Student");

        for (var i = 0; i < 5; i++)
            await Login(victim.Email, "wrong-password");

        (await Login(other.Email, other.Password)).Expect(200);
        for (var i = 0; i < 8; i++)
            (await Login("nobody-" + TestData.Unique() + "@example.test", "wrong-password")).Expect(401);   // never locked, never revealed
    }

    [Fact]
    public async Task A_successful_login_starts_the_count_again()
    {
        var user = await Data.UserAsync("Student");

        for (var round = 0; round < 3; round++)
        {
            for (var i = 0; i < 4; i++)
                (await Login(user.Email, "wrong-password")).Expect(401);

            (await Login(user.Email, user.Password)).Expect(200);   // four failures were not enough to lock
        }

        Assert.Equal("0", await Sql($"select \"AccessFailedCount\" from identity.users where \"Email\" = '{user.Email}'"));
    }

    [Fact]
    public async Task The_lock_expires_by_itself()
    {
        var user = await Data.UserAsync("Student");
        using var host = Factory.WithWebHostBuilder(b =>
        {
            b.UseSetting("Identity:Lockout:MaxAttempts", "2");
            b.UseSetting("Identity:Lockout:DurationMinutes", "0.03");   // about two seconds
        });
        var api = new Api(host.CreateClient());

        await Login(user.Email, "wrong-password", api);
        await Login(user.Email, "wrong-password", api);
        (await Login(user.Email, user.Password, api)).Expect(429);

        await Task.Delay(2600);

        (await Login(user.Email, user.Password, api)).Expect(200);
    }

    [Fact]
    public async Task An_administrator_reset_unlocks_the_account()
    {
        var user = await Data.UserAsync("Student");
        for (var i = 0; i < 5; i++)
            await Login(user.Email, "wrong-password");
        (await Login(user.Email, user.Password)).Expect(429);

        var reset = (await Api.PostAsync($"/api/auth/users/{user.UserId}/reset-password", null, Admin)).Expect(200);

        (await Login(user.Email, reset["temporaryPassword"].GetValue<string>())).Expect(200);
    }

    [Fact]
    public async Task A_deactivated_account_is_still_reported_as_deactivated_and_not_as_locked()
    {
        var studentId = await Data.StudentAsync();
        var user = await Data.StudentUserAsync(studentId);
        (await Api.PutAsync($"/api/students/{studentId}/status", new { status = "Withdrawn" }, Admin)).Expect(200);

        Assert.Equal(403, (await Login(user.Email, user.Password)).Code);
    }

    // ------------------------------------------------------------------ rate limit per address
    [Fact]
    public async Task Login_attempts_from_one_address_are_limited_and_the_answer_says_when_to_retry()
    {
        using var host = Factory.WithWebHostBuilder(b =>
        {
            b.UseSetting("RateLimiting:Auth:PermitLimit", "3");
            b.UseSetting("RateLimiting:Auth:WindowSeconds", "60");
        });
        using var client = host.CreateClient();
        var api = new Api(client);

        for (var i = 0; i < 3; i++)
            (await Login("nobody@example.test", "x", api)).Expect(401);

        using var response = await client.PostAsJsonAsync("/api/auth/login", new { email = "nobody@example.test", password = "x" });

        Assert.Equal(429, (int)response.StatusCode);
        Assert.True(response.Headers.RetryAfter is not null);
        Assert.Contains("Too many", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Token_refresh_shares_the_limit_and_other_endpoints_are_not_limited()
    {
        using var host = Factory.WithWebHostBuilder(b =>
        {
            b.UseSetting("RateLimiting:Auth:PermitLimit", "2");
            b.UseSetting("RateLimiting:Auth:WindowSeconds", "60");
        });
        var api = new Api(host.CreateClient());

        (await api.PostAsync("/api/auth/refresh", new { refreshToken = "x" })).Expect(401);
        (await api.PostAsync("/api/auth/refresh", new { refreshToken = "x" })).Expect(401);
        (await api.PostAsync("/api/auth/refresh", new { refreshToken = "x" })).Expect(429);

        for (var i = 0; i < 10; i++)
            (await api.GetAsync("/health")).Expect(200);
    }
}
