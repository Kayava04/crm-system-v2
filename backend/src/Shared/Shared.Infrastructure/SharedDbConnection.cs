using Npgsql;

namespace Shared.Infrastructure;

// One connection per request scope, used by every module DbContext.
// It stays closed (EF opens and closes it per operation) until a TransactionCoordinator pins it open for a transaction.
public sealed class SharedDbConnection(string connectionString) : IAsyncDisposable, IDisposable
{
    public NpgsqlConnection Connection { get; } = new(connectionString);

    public ValueTask DisposeAsync() => Connection.DisposeAsync();

    public void Dispose() => Connection.Dispose();
}
