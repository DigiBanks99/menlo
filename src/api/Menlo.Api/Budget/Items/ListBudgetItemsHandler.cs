using CSharpFunctionalExtensions;
using Menlo.Application.Budget;
using Menlo.Lib.Auth.Abstractions;
using Menlo.Lib.Auth.Errors;
using Menlo.Lib.Auth.Models;
using Menlo.Lib.Budget.Entities;
using Menlo.Lib.Budget.ValueObjects;
using Microsoft.EntityFrameworkCore;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Menlo.Api.Budget.Items;

public static class ListBudgetItemsHandler
{
    public static async Task<IResult> Handle(
        Guid budgetId,
        Guid categoryId,
        IUserContextProvider userContextProvider,
        IBudgetContext budgetContext,
        IConfiguration configuration,
        int? month = null,
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
            .Include(b => b.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

        if (budget is null || budget.HouseholdId != userContext.HouseholdId)
        {
            return Results.NotFound();
        }

        BudgetCategoryId catId = new(categoryId);
        IEnumerable<BudgetItem> items = budget.Items
            .Where(i => i.CategoryId == catId && !i.IsDeleted);

        if (month.HasValue)
        {
            items = items.Where(i => i.Month == month.Value);
        }

        List<BudgetItemDto> dtos = items
            .OrderBy(i => i.Month)
            .Select(BudgetItemMapper.MapToDto)
            .ToList();

        return Results.Ok(dtos);
    }
}
