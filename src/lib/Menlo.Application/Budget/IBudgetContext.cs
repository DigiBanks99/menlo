using Menlo.Lib.Budget.Entities;
using Microsoft.EntityFrameworkCore;
using BudgetAggregate = Menlo.Lib.Budget.Entities.Budget;

namespace Menlo.Application.Budget;

/// <summary>
/// The budget slice of the database: the household's budgets, one per calendar year, and its rental units, which
/// belong to the household rather than to a budget.
/// </summary>
public interface IBudgetContext
{
    DbSet<BudgetAggregate> Budgets { get; }

    DbSet<RentalUnit> RentalUnits { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
