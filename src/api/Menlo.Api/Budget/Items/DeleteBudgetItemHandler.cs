using CSharpFunctionalExtensions;
using Menlo.Application.Budget;
using Menlo.Lib.Auth.Abstractions;
using Menlo.Lib.Auth.Errors;
using Menlo.Lib.Auth.Models;
using Menlo.Lib.Budget.Errors;
using Menlo.Lib.Budget.ValueObjects;
using Menlo.Lib.Common.Abstractions;
using Microsoft.EntityFrameworkCore;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Menlo.Api.Budget.Items;

public static class DeleteBudgetItemHandler
{
    public static async Task<IResult> Handle(
        Guid budgetId,
        Guid categoryId,
        Guid itemId,
        IUserContextProvider userContextProvider,
        IBudgetContext budgetContext,
        ISoftDeleteStampFactory stampFactory,
        IConfiguration configuration,
        CancellationToken cancellationToken)
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
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

        if (budget is null || budget.HouseholdId != userContext.HouseholdId)
        {
            return Results.NotFound();
        }

        BudgetItemId budgetItemId = new(itemId);
        UnitResult<BudgetError> result = budget.DeleteItem(budgetItemId, stampFactory);

        if (result.IsFailure)
        {
            return BudgetItemMapper.MapError(result.Error);
        }

        await budgetContext.SaveChangesAsync(cancellationToken);

        return Results.NoContent();
    }
}
