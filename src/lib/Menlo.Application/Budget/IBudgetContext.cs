using Menlo.Lib.Budget.Entities;
using Microsoft.EntityFrameworkCore;
using BudgetAggregate = Menlo.Lib.Budget.Entities.Budget;

namespace Menlo.Application.Budget;

public interface IBudgetContext
{
    DbSet<BudgetAggregate> Budgets { get; }
    DbSet<CategoryNode> BudgetCategories { get; }
    DbSet<CanonicalCategory> CanonicalCategories { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
