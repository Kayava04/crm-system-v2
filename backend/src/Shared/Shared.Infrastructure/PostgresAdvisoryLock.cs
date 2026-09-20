using System.Text;
using Npgsql;
using Shared.Kernel.Abstractions;

namespace Shared.Infrastructure;

// Session-level advisory lock on its own connection: it is held for exactly as long as the handle lives
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
                // Closing the session releases the lock anyway, even if the unlock above failed
                await connection.DisposeAsync();
            }
        }
    }
}

public static class AdvisoryLockKey
{
    // Stable 64-bit key of a lock name (FNV-1a), the same on every instance and every run
    public static long For(string name)
    {
        var hash = 14695981039346656037UL;

        foreach (var b in Encoding.UTF8.GetBytes(name))
            hash = (hash ^ b) * 1099511628211UL;

        return unchecked((long)hash);
    }
}
