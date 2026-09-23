using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Crm.IntegrationTests.Tests;

// A wrongly configured application must refuse to start with a clear message, not fail later on the first login
public class StartupTests(CrmApiFactory factory) : ApiTest(factory)
{
    [Theory]
    [InlineData("")]
    [InlineData("too-short")]
    [InlineData("0123456789012345678901234567890")]   // 31 characters
    public void A_jwt_key_shorter_than_32_characters_stops_the_application_at_start_up(string key)
    {
        using var broken = Factory.WithWebHostBuilder(builder => builder.UseSetting("Jwt:SecretKey", key));

        var error = Assert.ThrowsAny<Exception>(() => broken.CreateClient());

        Assert.Contains("Jwt:SecretKey must be at least 32 characters", Flatten(error));
    }

    [Fact]
    public void A_missing_connection_string_stops_the_application_at_start_up()
    {
        using var broken = Factory.WithWebHostBuilder(builder => builder.UseSetting("ConnectionStrings:Default", ""));

        // an empty connection string is rejected by the shared infrastructure before any request is served
        Assert.ThrowsAny<Exception>(() => broken.CreateClient());
    }

    [Fact]
    public async Task The_application_starts_and_seeds_an_empty_database_by_itself()
    {
        // the factory built a brand new database: migrations, roles, permissions and the SuperAdmin all came from start-up
        Assert.Equal("4", await Sql("select count(*) from identity.roles"));
        Assert.Equal("1", await Sql($"select count(*) from identity.users where \"Email\" = '{CrmApiFactory.AdminEmail}'"));
        Assert.Equal("9", await Sql("select count(distinct table_schema) from information_schema.tables where table_schema in ('identity','students','teachers','courses','enrollments','scheduling','billing','notifications','materials')"));
    }

    [Fact]
    public async Task Starting_a_second_time_on_an_already_prepared_database_changes_nothing()
    {
        var before = await Sql("select (select count(*) from identity.permissions) || '/' || (select count(*) from identity.roles) || '/' || (select count(*) from identity.role_permissions) || '/' || (select count(*) from identity.users)");

        using var again = Factory.WithWebHostBuilder(_ => { });
        using var client = again.CreateClient();
        (await client.GetAsync("/health/ready")).EnsureSuccessStatusCode();

        Assert.Equal(before, await Sql("select (select count(*) from identity.permissions) || '/' || (select count(*) from identity.roles) || '/' || (select count(*) from identity.role_permissions) || '/' || (select count(*) from identity.users)"));
    }

    [Fact]
    public async Task The_frequently_used_lookups_have_indexes()
    {
        var indexes = await Sql(@"select string_agg(indexname, ',') from pg_indexes
            where schemaname in ('enrollments','students','teachers','scheduling','notifications','billing')");

        foreach (var expected in new[]
                 {
                     "IX_enrollments_StudentId", "IX_enrollments_CourseId", "IX_students_UserId", "IX_teachers_UserId",
                     "IX_schedules_EnrollmentId", "IX_schedules_GroupId", "IX_schedules_TeacherId_ScheduledDate",
                     "IX_notifications_RecipientUserId_ReadAt", "IX_student_invoices_EnrollmentId_Period"
                 })
            Assert.Contains(expected, indexes!);
    }

    // Seq is only reachable at "localhost" when the API runs on the host; inside a Docker container it is
    // reachable only by its compose service name. docker-compose.yml overrides the same config key with an
    // env var shaped exactly like this. If this breaks, the override in docker-compose.yml lost its target.
    [Fact]
    public void The_seq_sink_is_configured_in_every_environment_and_its_address_can_be_overridden()
    {
        var defaultUrl = Factory.Services.GetRequiredService<IConfiguration>()["Serilog:WriteTo:2:Args:serverUrl"];
        Assert.Equal("http://localhost:5341", defaultUrl);
        Assert.Equal("Seq", Factory.Services.GetRequiredService<IConfiguration>()["Serilog:WriteTo:2:Name"]);

        using var inContainer = Factory.WithWebHostBuilder(builder =>
            builder.UseSetting("Serilog:WriteTo:2:Args:serverUrl", "http://seq:5341"));

        Assert.Equal("http://seq:5341", inContainer.Services.GetRequiredService<IConfiguration>()["Serilog:WriteTo:2:Args:serverUrl"]);
    }

    private static string Flatten(Exception ex)
    {
        var text = new System.Text.StringBuilder();
        for (var current = ex; current is not null; current = current.InnerException!)
        {
            text.AppendLine(current.Message);
            if (current.InnerException is null) break;
        }

        return text.ToString();
    }
}
