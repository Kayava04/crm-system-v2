using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace Host.Extensions;

// Readiness: the database answers and every module schema exists (i.e. migrations were applied)
internal sealed class DatabaseHealthCheck(IConfiguration configuration) : IHealthCheck
{
    private static readonly string[] ExpectedSchemas =
    [
        "identity", "students", "teachers", "courses", "enrollments",
        "scheduling", "billing", "notifications", "materials"
    ];

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new NpgsqlConnection(configuration.GetConnectionString("Default"));
            await connection.OpenAsync(cancellationToken);

            await using var command = new NpgsqlCommand(
                "select schema_name from information_schema.schemata where schema_name = any(@expected)", connection);
            command.Parameters.AddWithValue("expected", ExpectedSchemas);

            var found = new HashSet<string>();
            await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                    found.Add(reader.GetString(0));
            }

            var missing = ExpectedSchemas.Where(s => !found.Contains(s)).ToList();

            return missing.Count == 0
                ? HealthCheckResult.Healthy("Database is reachable and all module schemas exist.")
                : HealthCheckResult.Unhealthy($"Database is reachable but schemas are missing: {string.Join(", ", missing)}. Apply the migrations.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database is not reachable.", ex);
        }
    }
}
