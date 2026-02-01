using Menlo.Lib.Budget.Entities;
using Menlo.Lib.Common.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Menlo.Api.Persistence.Interceptors;

/// <summary>
/// Interceptor that synchronizes Money value objects with shadow properties.
/// Handles the PlannedAmount property on BudgetCategory.
/// </summary>
public sealed class MoneyPropertyInterceptor : SaveChangesInterceptor
{
    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
        {
            SyncMoneyToShadowProperties(eventData.Context);
        }

        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            SyncMoneyToShadowProperties(eventData.Context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void SyncMoneyToShadowProperties(DbContext context)
    {
        IEnumerable<EntityEntry<BudgetCategory>> entries = context.ChangeTracker
            .Entries<BudgetCategory>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified);

        foreach (EntityEntry<BudgetCategory> entry in entries)
        {
            Money? plannedAmount = entry.Entity.PlannedAmount;

            if (plannedAmount.HasValue)
            {
                entry.Property("PlannedAmountValue").CurrentValue = plannedAmount.Value.Amount;
                entry.Property("PlannedAmountCurrency").CurrentValue = plannedAmount.Value.Currency;
            }
            else
            {
                entry.Property("PlannedAmountValue").CurrentValue = null;
                entry.Property("PlannedAmountCurrency").CurrentValue = null;
            }
        }
    }
}
