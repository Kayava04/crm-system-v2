using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Crm.IntegrationTests.Infrastructure;

// Every test run gets its own empty database on the Postgres server that is already used for development.
// The server is taken from CRM_TEST_CONNECTION, or from the Host project's user secrets (ConnectionStrings:Default).
public sealed class TestDatabase : IAsyncDisposable
{
    private const string HostUserSecretsId = "8467240a-6aba-4429-9e9d-262ff16e9380";

    private readonly NpgsqlConnectionStringBuilder _server;

    public string Name { get; } = "crm_tests_" + Guid.NewGuid().ToString("N")[..10];

    public string ConnectionString => new NpgsqlConnectionStringBuilder(_server.ConnectionString) { Database = Name }.ConnectionString;

    private TestDatabase(NpgsqlConnectionStringBuilder server) => _server = server;

    public static async Task<TestDatabase> CreateAsync()
    {
        var baseConnection = Environment.GetEnvironmentVariable("CRM_TEST_CONNECTION")
            ?? new ConfigurationBuilder().AddUserSecrets(HostUserSecretsId).Build()["ConnectionStrings:Default"]
            ?? throw new InvalidOperationException(
                "No database server for the tests. Set CRM_TEST_CONNECTION or the Host user secret ConnectionStrings:Default.");

        var database = new TestDatabase(new NpgsqlConnectionStringBuilder(baseConnection) { Pooling = false });

        // Connect to the maintenance database, the target one does not exist yet
        await using var connection = new NpgsqlConnection(
            new NpgsqlConnectionStringBuilder(database._server.ConnectionString) { Database = "postgres" }.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"create database \"{database.Name}\"", connection);
        await command.ExecuteNonQueryAsync();

        return database;
    }

    public async Task<string?> ScalarAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);

        return (await command.ExecuteScalarAsync())?.ToString();
    }

    public async Task ExecuteAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    public async ValueTask DisposeAsync()
    {
        NpgsqlConnection.ClearAllPools();

        await using var connection = new NpgsqlConnection(
            new NpgsqlConnectionStringBuilder(_server.ConnectionString) { Database = "postgres" }.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"drop database if exists \"{Name}\" with (force)", connection);
        await command.ExecuteNonQueryAsync();
    }
}
