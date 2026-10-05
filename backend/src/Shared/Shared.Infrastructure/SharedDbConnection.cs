using Npgsql;

namespace Shared.Infrastructure;

public sealed class SharedDbConnection(string connectionString) : IAsyncDisposable, IDisposable
{
    public NpgsqlConnection Connection { get; } = new(connectionString);

    public ValueTask DisposeAsync() => Connection.DisposeAsync();

    public void Dispose() => Connection.Dispose();
}
