using System.Text;
using Npgsql;
using Shared.Kernel.Abstractions;

namespace Shared.Infrastructure;

public sealed class PostgresAdvisoryLock(string connectionString) : IAdvisoryLock
{
    public async Task<IAsyncDisposable?> TryAcquireAsync(string name, CancellationToken ct = default)
    {
        var connection = new NpgsqlConnection(connectionString);

        try
        {
            await connection.OpenAsync(ct);

            await using var command = new NpgsqlCommand("select pg_try_advisory_lock(@key)", connection);
            command.Parameters.AddWithValue("key", AdvisoryLockKey.For(name));

            if (await command.ExecuteScalarAsync(ct) is true)
                return new Handle(connection, AdvisoryLockKey.For(name));
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }

        await connection.DisposeAsync();

        return null;
    }

    private sealed class Handle(NpgsqlConnection connection, long key) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            try
            {
                await using var command = new NpgsqlCommand("select pg_advisory_unlock(@key)", connection);
                command.Parameters.AddWithValue("key", key);
                await command.ExecuteScalarAsync();
            }
            finally
            {
                await connection.DisposeAsync();
            }
        }
    }
}

public static class AdvisoryLockKey
{
    public static long For(string name)
    {
        var hash = 14695981039346656037UL;

        foreach (var b in Encoding.UTF8.GetBytes(name))
            hash = (hash ^ b) * 1099511628211UL;

        return unchecked((long)hash);
    }
}
