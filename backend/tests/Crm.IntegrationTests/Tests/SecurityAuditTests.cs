using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Crm.IntegrationTests.Tests;

// Inspects every route of the running application instead of trusting that each endpoint was written correctly
public class SecurityAuditTests(CrmApiFactory factory) : ApiTest(factory)
{
    private static readonly string[] PublicApiEndpoints = ["POST /api/auth/login", "POST /api/auth/refresh"];

    private List<(string Route, RouteEndpoint Endpoint)> ApiEndpoints() =>
        Factory.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Where(e => e.RoutePattern.RawText!.StartsWith("/api/", StringComparison.Ordinal))
            .SelectMany(e => (e.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? []).Select(m => (Route: $"{m} {e.RoutePattern.RawText}", Endpoint: e)))
            .ToList();

    [Fact]
    public void The_audit_really_sees_the_whole_api()
    {
        var endpoints = ApiEndpoints();

        Assert.True(endpoints.Count >= 100, $"only {endpoints.Count} endpoints found");
        Assert.Contains("GET /api/calendar/my", endpoints.Select(e => e.Route));
        Assert.Contains("POST /api/students/import", endpoints.Select(e => e.Route));
        Assert.Contains("PUT /api/schedules/reassign-teacher", endpoints.Select(e => e.Route));
    }

    [Fact]
    public void Every_api_endpoint_requires_authentication_except_login_and_refresh()
    {
        var open = ApiEndpoints()
            .Where(e => !e.Endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Any()
                        || e.Endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null)
            .Select(e => e.Route)
            .Order()
            .ToList();

        Assert.Equal(PublicApiEndpoints.Order(), open);
    }

    [Fact]
    public async Task Every_policy_an_endpoint_asks_for_exists()
    {
        var provider = Factory.Services.GetRequiredService<IAuthorizationPolicyProvider>();
        var missing = new List<string>();

        foreach (var (route, endpoint) in ApiEndpoints())
            foreach (var data in endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Where(d => !string.IsNullOrEmpty(d.Policy)))
                if (await provider.GetPolicyAsync(data.Policy!) is null)
                    missing.Add($"{route} -> {data.Policy}");

        Assert.Empty(missing);
    }

    [Fact]
    public async Task Every_permission_has_a_policy_and_is_seeded_in_the_database()
    {
        var provider = Factory.Services.GetRequiredService<IAuthorizationPolicyProvider>();

        foreach (var permission in Enum.GetNames<Identity.Contracts.Enums.SystemPermission>())
        {
            Assert.NotNull(await provider.GetPolicyAsync(permission));
            Assert.Equal("1", await Sql($"select count(*) from identity.permissions where \"Name\" = '{permission}'"));
        }
    }

    [Fact]
    public void Endpoints_have_unique_names_and_no_duplicate_routes()
    {
        var endpoints = ApiEndpoints();

        Assert.Equal(endpoints.Count, endpoints.Select(e => e.Route).Distinct().Count());

        var names = endpoints
            .Select(e => e.Endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName)
            .Distinct()
            .ToList();
        Assert.DoesNotContain(null, names);
        Assert.All(endpoints.GroupBy(e => e.Endpoint), g => Assert.NotNull(g.Key.Metadata.GetMetadata<IEndpointNameMetadata>()));
    }

    [Fact]
    public async Task Every_protected_endpoint_answers_401_to_an_anonymous_caller_and_never_500()
    {
        var failures = new List<string>();

        foreach (var (route, endpoint) in ApiEndpoints().Where(e => !PublicApiEndpoints.Contains(e.Route)))
        {
            var method = route[..route.IndexOf(' ')];
            var path = System.Text.RegularExpressions.Regex.Replace(route[(route.IndexOf(' ') + 1)..], @"\{[^}]+\}", Guid.NewGuid().ToString());

            var response = await Api.SendAsync(new HttpMethod(method), path);

            if (response.Code != 401)
                failures.Add($"{route} -> {response.Code}");
        }

        Assert.Empty(failures);
    }

    [Fact]
    public async Task No_endpoint_returns_a_server_error_for_empty_or_garbage_input_when_authorised()
    {
        var failures = new List<string>();

        foreach (var (route, _) in ApiEndpoints().Where(e => !PublicApiEndpoints.Contains(e.Route) && e.Route.StartsWith("P")))   // POST / PUT / PATCH
        {
            var method = route[..route.IndexOf(' ')];
            var path = System.Text.RegularExpressions.Regex.Replace(route[(route.IndexOf(' ') + 1)..], @"\{[^}]+\}", Guid.NewGuid().ToString());

            foreach (var body in new object?[] { null, new { }, new { x = 1 }, new[] { 1, 2 } })
            {
                var response = await Api.SendAsync(new HttpMethod(method), path, body, Admin);

                if (response.Code >= 500)
                    failures.Add($"{route} with {System.Text.Json.JsonSerializer.Serialize(body)} -> {response.Code}");
            }
        }

        Assert.Empty(failures);
    }
}
