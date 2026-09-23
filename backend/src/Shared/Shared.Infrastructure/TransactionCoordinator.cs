using Microsoft.EntityFrameworkCore;
using Npgsql;
using Shared.Kernel.Abstractions;

namespace Shared.Infrastructure;

public sealed class TransactionCoordinator(SharedDbConnection shared) : ITransactionCoordinator
{
    // The interceptor that enlists DbContexts is a singleton, so it finds the running transaction through the async flow
    private static readonly AsyncLocal<TransactionCoordinator?> Ambient = new();

    private readonly List<DbContext> _enlisted = [];
    private NpgsqlTransaction? _transaction;

    internal static TransactionCoordinator? Current => Ambient.Value;

    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct = default)
    {
        // Nested calls simply join the transaction that is already running
        if (_transaction is not null)
            return await action(ct);

        var connection = shared.Connection;
        var openedHere = connection.State != System.Data.ConnectionState.Open;

        if (openedHere)
            await connection.OpenAsync(ct);

        _transaction = await connection.BeginTransactionAsync(ct);
        Ambient.Value = this;

        try
        {
            var result = await action(ct);
            await _transaction.CommitAsync(ct);

            return result;
        }
        catch
        {
            try { await _transaction.RollbackAsync(CancellationToken.None); }
            catch { /* the connection may already be broken; the original error matters more */ }

            throw;
        }
        finally
        {
            Ambient.Value = null;

            // Contexts must forget the finished transaction, otherwise a later save in the same request would reuse it
            foreach (var context in _enlisted)
                await context.Database.UseTransactionAsync(null, CancellationToken.None);

            _enlisted.Clear();

            await _transaction.DisposeAsync();
            _transaction = null;

            if (openedHere)
                await connection.CloseAsync();
        }
    }

    public async Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken ct = default) =>
        await ExecuteAsync<object?>(async token =>
        {
            await action(token);
            return null;
        }, ct);

    public async Task AcquireLockAsync(string name, CancellationToken ct = default)
    {
        if (_transaction is null)
            throw new InvalidOperationException("A lock can only be taken inside ITransactionCoordinator.ExecuteAsync.");

        await using var command = new NpgsqlCommand("select pg_advisory_xact_lock(@key)", shared.Connection, _transaction);
        command.Parameters.AddWithValue("key", AdvisoryLockKey.For(name));
        await command.ExecuteNonQueryAsync(ct);
    }

    internal async Task EnlistAsync(DbContext context, CancellationToken ct)
    {
        if (_transaction is null || context.Database.CurrentTransaction is not null)
            return;

        await context.Database.UseTransactionAsync(_transaction, ct);
        _enlisted.Add(context);
    }

    internal void Enlist(DbContext context)
    {
        if (_transaction is null || context.Database.CurrentTransaction is not null)
            return;

        context.Database.UseTransaction(_transaction);
        _enlisted.Add(context);
    }
}
