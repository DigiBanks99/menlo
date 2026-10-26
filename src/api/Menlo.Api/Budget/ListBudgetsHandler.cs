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

namespace Menlo.Api.Budget;

/// <summary>
/// Handler for the GET /api/budgets endpoint.
/// Returns every budget of the authenticated user's household, newest year first.
/// </summary>
public static class ListBudgetsHandler
{
    private const string Currency = "ZAR";

    public static async Task<IResult> Handle(
        IUserContextProvider userContextProvider,
        IBudgetContext budgetContext,
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        if (!IsFeatureEnabled(configuration))
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
        int currentMonth = DateTime.UtcNow.Month;

        IQueryable<Lib.Budget.Entities.Budget> query = budgetContext.Budgets
            .Include(b => b.Categories);

        // Item figures are only exposed while budget items are enabled; only this month's items are needed
        if (configuration.GetValue<bool>("Features:BudgetItems", defaultValue: false))
        {
            query = query.Include(b => b.Items.Where(i => i.Month == currentMonth && !i.IsDeleted));
        }

        List<Lib.Budget.Entities.Budget> budgets = await query
            .AsNoTracking()
            .AsSplitQuery()
            .Where(b => b.HouseholdId == userContext.HouseholdId)
            .OrderByDescending(b => b.Year)
            .ToListAsync(cancellationToken);

        return Results.Ok(budgets.Select(b => MapToListItem(b, currentMonth)).ToList());
    }

    private static BudgetListItem MapToListItem(Lib.Budget.Entities.Budget budget, int month)
    {
        HashSet<BudgetCategoryId> activeCategoryIds = budget.Categories
            .Where(c => !c.IsDeleted)
            .Select(c => c.Id)
            .ToHashSet();

        // Mirrors the summary's expense totals: non-deleted expense items in active categories
        List<BudgetItem> expenses = budget.Items
            .Where(i => !i.IsDeleted
                        && i.Month == month
                        && i.BudgetFlow == BudgetFlow.Expense
                        && activeCategoryIds.Contains(i.CategoryId))
            .ToList();

        decimal planned = expenses.Sum(i => i.PlannedAmount.Amount);
        MoneyDto? spent = expenses.Any(i => i.SpentAmount is not null)
            ? new MoneyDto(expenses.Sum(i => i.SpentAmount?.Amount ?? 0m), Currency)
            : null;

        return new BudgetListItem(
            Id: budget.Id.Value,
            Year: budget.Year,
            Status: budget.Status.ToString(),
            PlannedExpenses: new MoneyDto(planned, Currency),
            SpentExpenses: spent,
            CategoryCount: activeCategoryIds.Count);
    }

    private static bool IsFeatureEnabled(IConfiguration configuration) =>
        configuration.GetValue<bool>("Features:Budget", defaultValue: false);
}
