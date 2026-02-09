using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Common.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Menlo.Api.Persistence.Interceptors;

/// <summary>
/// EF Core interceptor that automatically stamps audit information on entities
/// implementing <see cref="IAuditable"/> during SaveChanges.
/// </summary>
public sealed class AuditingInterceptor : SaveChangesInterceptor
{
    private readonly IAuditStampFactory _auditStampFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditingInterceptor"/> class.
    /// </summary>
    /// <param name="auditStampFactory">Factory for creating audit stamps.</param>
    public AuditingInterceptor(IAuditStampFactory auditStampFactory)
    {
        _auditStampFactory = auditStampFactory;
    }

    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ApplyAuditStamps(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyAuditStamps(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyAuditStamps(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        foreach (var entry in context.ChangeTracker.Entries<IAuditable>())
        {
            AuditOperation? operation = entry.State switch
            {
                EntityState.Added => AuditOperation.Create,
                EntityState.Modified => AuditOperation.Update,
                _ => null
            };

            if (operation is not null)
            {
                entry.Entity.Audit(_auditStampFactory, operation.Value);
            }
        }
    }
}
