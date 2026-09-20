using System.Net;

namespace Crm.IntegrationTests.Tests;

public class HealthAndCorsTests(CrmApiFactory factory) : ApiTest(factory)
{
    [Fact]
    public async Task Liveness_answers_without_authentication()
    {
        var response = (await Api.GetAsync("/health")).Expect(200);

        Assert.Equal("Healthy", response["status"].GetValue<string>());
    }

    [Fact]
    public async Task Readiness_reports_the_database_and_all_module_schemas()
    {
        var response = (await Api.GetAsync("/health/ready")).Expect(200);

        Assert.Equal("Healthy", response["status"].GetValue<string>());
        var check = response["checks"].AsArray().Single();
        Assert.Equal("database", check!["name"]!.GetValue<string>());
        Assert.Contains("all module schemas exist", check["description"]!.GetValue<string>());
    }

    [Fact]
    public async Task Readiness_is_unhealthy_when_a_module_schema_is_missing()
    {
        try
        {
            await Factory.Database.ExecuteAsync("alter schema billing rename to billing_off");

            var response = (await Api.GetAsync("/health/ready")).Expect(503);

            Assert.Equal("Unhealthy", response["status"].GetValue<string>());
            Assert.Contains("billing", response["checks"].AsArray().Single()!["description"]!.GetValue<string>());
        }
        finally
        {
            await Factory.Database.ExecuteAsync("alter schema billing_off rename to billing");
        }
    }

    [Fact]
    public async Task Preflight_from_the_frontend_origin_is_allowed()
    {
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/auth/login");
        request.Headers.Add("Origin", "http://localhost:5173");
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "authorization,content-type");

        using var response = await Api.Http.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("http://localhost:5173", response.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.Contains("POST", response.Headers.GetValues("Access-Control-Allow-Methods").Single());
        Assert.Contains("authorization", response.Headers.GetValues("Access-Control-Allow-Headers").Single(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Preflight_from_a_foreign_origin_is_not_allowed()
    {
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/auth/login");
        request.Headers.Add("Origin", "http://evil.example");
        request.Headers.Add("Access-Control-Request-Method", "POST");

        using var response = await Api.Http.SendAsync(request);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Normal_responses_carry_the_cors_header_for_the_frontend_origin()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("Origin", "http://localhost:5173");

        using var response = await Api.Http.SendAsync(request);

        Assert.Equal("http://localhost:5173", response.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }

    [Fact]
    public async Task Protected_endpoints_reject_anonymous_calls()
    {
        (await Api.GetAsync("/api/students")).Expect(401);
        (await Api.GetAsync("/api/calendar/my")).Expect(401);
    }
}
