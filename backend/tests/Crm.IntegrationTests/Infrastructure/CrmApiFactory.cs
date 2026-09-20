using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Crm.IntegrationTests.Infrastructure;

// The whole application (all modules, real Postgres, real migrations, real JWT) hosted in memory
public sealed class CrmApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string AdminEmail = "test-admin@example.test";
    public const string AdminPassword = "Test-Admin-123!";

    public TestDatabase Database { get; private set; } = null!;


    public async Task InitializeAsync()
    {
        Database = await TestDatabase.CreateAsync();

        // Starts the host (migrations and seeding run here) so the first test does not pay for it
        using var client = CreateClient();
        var health = await client.GetAsync("/health/ready");
        health.EnsureSuccessStatusCode();
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await Database.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // UseSetting is visible to Program.cs while it registers services (unlike ConfigureAppConfiguration)
        builder.UseSetting("ConnectionStrings:Default", Database.ConnectionString);
        builder.UseSetting("Jwt:SecretKey", "integration-tests-secret-key-that-is-long-enough-1234567890");
        builder.UseSetting("SuperAdmin:Email", AdminEmail);
        builder.UseSetting("SuperAdmin:Password", AdminPassword);
        builder.UseSetting("Database:MigrateOnStartup", "true");
        // the whole suite logs in from one "address": the limit is tested on its own host
        builder.UseSetting("RateLimiting:Auth:PermitLimit", "100000");
        builder.UseSetting("Cors:AllowedOrigins:0", "http://localhost:5173");
        builder.UseSetting("Serilog:MinimumLevel:Default", "Warning");
        builder.UseSetting("Serilog:WriteTo:1:Args:path", Path.Combine(Path.GetTempPath(), "crm-tests-.log"));
    }
}

[CollectionDefinition("Api")]
public sealed class ApiCollection : ICollectionFixture<CrmApiFactory>;
