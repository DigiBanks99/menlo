using Microsoft.EntityFrameworkCore;
using BudgetAggregate = Menlo.Lib.Budget.Entities.Budget;

namespace Menlo.Application.Budget;

/// <summary>
/// The budget slice of the database: the household's budgets, one per calendar year. A budget loads with its
/// categories, which are changed only through it.
/// </summary>
public interface IBudgetContext
{
    DbSet<BudgetAggregate> Budgets { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
