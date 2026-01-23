using Menlo.Lib.Common.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Menlo.Api.Persistence.Interceptors;

/// <summary>
/// EF Core interceptor that handles soft delete cascade operations.
/// When a parent entity is soft deleted, this interceptor automatically
/// soft deletes related child entities via navigation properties.
/// </summary>
public sealed class SoftDeleteInterceptor : SaveChangesInterceptor
{
    private readonly IAuditStampFactory _auditStampFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="SoftDeleteInterceptor"/> class.
    /// </summary>
    /// <param name="auditStampFactory">Factory for creating audit stamps.</param>
    public SoftDeleteInterceptor(IAuditStampFactory auditStampFactory)
    {
        _auditStampFactory = auditStampFactory;
    }

    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        CascadeSoftDelete(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        CascadeSoftDelete(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void CascadeSoftDelete(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        // Track entities that have been processed to prevent infinite loops
        HashSet<object> processed = [];

        foreach (var entry in context.ChangeTracker.Entries<ISoftDeletable>())
        {
            // Only process modified entities that are being soft deleted
            if (entry.State != EntityState.Modified)
            {
                continue;
            }

            ISoftDeletable entity = entry.Entity;
            if (!entity.IsDeleted || processed.Contains(entity))
            {
                continue;
            }

            processed.Add(entity);

            // Cascade soft delete to navigation collections
            foreach (var navigation in entry.Navigations.Where(n => n.IsLoaded))
            {
                if (navigation.CurrentValue is IEnumerable<ISoftDeletable> children)
                {
                    foreach (ISoftDeletable child in children)
                    {
                        if (!child.IsDeleted && !processed.Contains(child))
                        {
                            child.SoftDelete(_auditStampFactory);
                            processed.Add(child);
                        }
                    }
                }
                else if (navigation.CurrentValue is ISoftDeletable child)
                {
                    if (!child.IsDeleted && !processed.Contains(child))
                    {
                        child.SoftDelete(_auditStampFactory);
                        processed.Add(child);
                    }
                }
            }
        }
    }
}
