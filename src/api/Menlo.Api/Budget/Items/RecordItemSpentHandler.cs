using CSharpFunctionalExtensions;
using Menlo.Application.Budget;
using Menlo.Lib.Auth.Abstractions;
using Menlo.Lib.Auth.Errors;
using Menlo.Lib.Auth.Models;
using Menlo.Lib.Budget.Entities;
using Menlo.Lib.Budget.Errors;
using Menlo.Lib.Budget.ValueObjects;
using Menlo.Lib.Common.ValueObjects;
using Microsoft.EntityFrameworkCore;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Menlo.Api.Budget.Items;

public static class RecordItemSpentHandler
{
    public static async Task<IResult> Handle(
        Guid budgetId,
        Guid categoryId,
        Guid itemId,
        RecordBudgetItemSpentRequest request,
        IUserContextProvider userContextProvider,
        IBudgetContext budgetContext,
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
            .Include(b => b.Categories)
            .Include(b => b.Items)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

        if (budget is null || budget.HouseholdId != userContext.HouseholdId)
        {
            return Results.NotFound();
        }

        Result<Money, Menlo.Lib.Common.Abstractions.Error> moneyResult =
            Money.Create(request.Amount, request.Currency);
        if (moneyResult.IsFailure)
        {
            return Results.Problem(
                detail: moneyResult.Error.Message,
                statusCode: 400,
                title: "Invalid amount");
        }

        BudgetItemId budgetItemId = new(itemId);
        Result<BudgetItem, BudgetError> result = budget.RecordItemSpent(budgetItemId, moneyResult.Value);

        if (result.IsFailure)
        {
            return BudgetItemMapper.MapError(result.Error);
        }

        await budgetContext.SaveChangesAsync(cancellationToken);

        BudgetItemDto dto = BudgetItemMapper.MapToDto(result.Value);
        return Results.Ok(dto);
    }
}
