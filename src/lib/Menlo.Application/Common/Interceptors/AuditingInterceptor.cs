using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Common.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Menlo.Application.Common.Interceptors;

internal sealed class AuditingInterceptor(IAuditStampFactory factory) : SaveChangesInterceptor
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

        foreach (EntityEntry<IAuditable> entry in eventData.Context.ChangeTracker.Entries<IAuditable>())
        {
            AuditOperation? operation = entry.State switch
            {
                EntityState.Added => AuditOperation.Create,
                EntityState.Modified => AuditOperation.Update,
                _ => null
            };

            if (operation.HasValue)
            {
                entry.Entity.Audit(factory, operation.Value);
            }
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}


