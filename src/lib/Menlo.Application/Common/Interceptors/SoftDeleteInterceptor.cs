using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Common.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Menlo.Application.Common.Interceptors;

internal sealed class SoftDeleteInterceptor(ISoftDeleteStampFactory factory) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null)
        {
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        foreach (EntityEntry<ISoftDeletable>? entry in eventData.Context.ChangeTracker
            .Entries<ISoftDeletable>()
            .Where(e => e.State == EntityState.Deleted))
        {
            entry.State = EntityState.Modified;
            SoftDeleteStamp stamp = factory.CreateStamp();
            entry.Entity.MarkDeleted(stamp.ActorId, stamp.Timestamp);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
