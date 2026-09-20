using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Shared.Infrastructure;

// Right before any module context saves, attach it to the running shared transaction (if there is one)
public sealed class TransactionEnlistmentInterceptor : SaveChangesInterceptor
{
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null && TransactionCoordinator.Current is { } coordinator)
            await coordinator.EnlistAsync(eventData.Context, cancellationToken);

        return result;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is not null && TransactionCoordinator.Current is { } coordinator)
            coordinator.Enlist(eventData.Context);

        return result;
    }
}
