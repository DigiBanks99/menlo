using CSharpFunctionalExtensions;
using Menlo.Application.Budget;
using Menlo.Lib.Auth.Abstractions;
using Menlo.Lib.Auth.Errors;
using Menlo.Lib.Auth.Models;
using Menlo.Lib.Budget.Entities;
using Menlo.Lib.Budget.Enums;
using Menlo.Lib.Budget.ValueObjects;
using Microsoft.EntityFrameworkCore;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Menlo.Api.Budget.Summary;

/// <summary>
/// Handler for the GET /api/budgets/{budgetId}/summary endpoint.
/// Returns the aggregated balance-sheet summary for a budget month.
/// </summary>
public static class GetBudgetSummaryHandler
{
    public static async Task<IResult> Handle(
        Guid budgetId,
        IUserContextProvider userContextProvider,
        IBudgetContext budgetContext,
        IConfiguration configuration,
        int month,
        CancellationToken cancellationToken = default)
    {
        if (!configuration.GetValue<bool>("Features:BudgetItems", defaultValue: false))
        {
            return Results.NotFound();
        }

        Result<UserContext, AuthError> userContextResult =
            await userContextProvider.GetUserContextAsync(cancellationToken);

        if (userContextResult.IsFailure)
        {
            return Results.Unauthorized();
        }

        UserContext userContext = userContextResult.Value;
        BudgetId id = new(budgetId);

        Lib.Budget.Entities.Budget? budget = await budgetContext.Budgets
            .Include(b => b.Categories)
            .Include(b => b.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

        if (budget is null || budget.HouseholdId != userContext.HouseholdId)
        {
            return Results.NotFound();
        }

        List<BudgetItem> monthItems = budget.Items
            .Where(i => i.Month == month && !i.IsDeleted)
            .ToList();

        List<CategoryNode> activeCategories = budget.Categories
            .Where(c => !c.IsDeleted)
            .ToList();

        List<CategoryNode> rootCategories = activeCategories
            .Where(c => c.ParentId is null)
            .ToList();

        List<CategorySummaryDto> income = BuildCategorySummaries(
            rootCategories, activeCategories, monthItems, BudgetFlow.Income);

        List<CategorySummaryDto> expenses = BuildCategorySummaries(
            rootCategories, activeCategories, monthItems, BudgetFlow.Expense);

        decimal netPlanned = income.Sum(c => c.PlannedTotal) - expenses.Sum(c => c.PlannedTotal);

        decimal? totalIncomeRealized = SumNullableField(income, c => c.RealizedTotal);
        decimal? totalExpenseRealized = SumNullableField(expenses, c => c.RealizedTotal);
        decimal? netRealized = totalIncomeRealized.HasValue || totalExpenseRealized.HasValue
            ? (totalIncomeRealized ?? 0m) - (totalExpenseRealized ?? 0m)
            : null;

        decimal? totalIncomeSpent = SumNullableField(income, c => c.SpentTotal);
        decimal? totalExpenseSpent = SumNullableField(expenses, c => c.SpentTotal);
        decimal? netSpent = totalIncomeSpent.HasValue || totalExpenseSpent.HasValue
            ? (totalIncomeSpent ?? 0m) - (totalExpenseSpent ?? 0m)
            : null;

        var dto = new BudgetSummaryDto(
            BudgetId: budget.Id.Value,
            Year: budget.Year,
            Month: month,
            Income: income,
            Expenses: expenses,
            NetPlanned: netPlanned,
            NetRealized: netRealized,
            NetSpent: netSpent);

        return Results.Ok(dto);
    }

    private static List<CategorySummaryDto> BuildCategorySummaries(
        List<CategoryNode> rootCategories,
        List<CategoryNode> allCategories,
        List<BudgetItem> monthItems,
        BudgetFlow flow)
    {
        return rootCategories
            .Where(r => r.BudgetFlow == flow)
            .Select(root =>
            {
                List<CategoryNode> children = allCategories
                    .Where(c => c.ParentId == root.Id)
                    .ToList();

                List<CategorySummaryDto> childDtos = children
                    .Select(child =>
                    {
                        List<BudgetItem> childItems = monthItems
                            .Where(i => i.CategoryId == child.Id)
                            .ToList();

                        return new CategorySummaryDto(
                            Id: child.Id.Value,
                            Name: child.Name.Value,
                            PlannedTotal: childItems.Sum(i => i.PlannedAmount.Amount),
                            RealizedTotal: AggregateNullable(childItems, i => i.RealizedAmount?.Amount),
                            SpentTotal: AggregateNullable(childItems, i => i.SpentAmount?.Amount),
                            Children: []);
                    })
                    .ToList();

                // Root totals include items directly on the root + all children items
                List<BudgetItem> rootDirectItems = monthItems
                    .Where(i => i.CategoryId == root.Id)
                    .ToList();

                HashSet<BudgetCategoryId> childIds = children.Select(c => c.Id).ToHashSet();
                List<BudgetItem> allRootItems = monthItems
                    .Where(i => i.CategoryId == root.Id || childIds.Contains(i.CategoryId))
                    .ToList();

                return new CategorySummaryDto(
                    Id: root.Id.Value,
                    Name: root.Name.Value,
                    PlannedTotal: allRootItems.Sum(i => i.PlannedAmount.Amount),
                    RealizedTotal: AggregateNullable(allRootItems, i => i.RealizedAmount?.Amount),
                    SpentTotal: AggregateNullable(allRootItems, i => i.SpentAmount?.Amount),
                    Children: childDtos);
            })
            .ToList();
    }

    private static decimal? AggregateNullable(List<BudgetItem> items, Func<BudgetItem, decimal?> selector)
    {
        bool anyHasValue = items.Any(i => selector(i).HasValue);
        if (!anyHasValue)
        {
            return null;
        }

        return items.Sum(i => selector(i) ?? 0m);
    }

    private static decimal? SumNullableField(
        List<CategorySummaryDto> categories,
        Func<CategorySummaryDto, decimal?> selector)
    {
        bool anyHasValue = categories.Any(c => selector(c).HasValue);
        if (!anyHasValue)
        {
            return null;
        }

        return categories.Sum(c => selector(c) ?? 0m);
    }
}
