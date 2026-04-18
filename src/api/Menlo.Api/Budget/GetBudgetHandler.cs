using CSharpFunctionalExtensions;
using Menlo.Application.Budget;
using Menlo.Lib.Auth.Abstractions;
using Menlo.Lib.Auth.Errors;
using Menlo.Lib.Auth.Models;
using Menlo.Lib.Budget.ValueObjects;
using Microsoft.EntityFrameworkCore;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Menlo.Api.Budget;

/// <summary>
/// Handler for the GET /api/budgets/{id} endpoint.
/// Returns the budget if it belongs to the authenticated user's household.
/// </summary>
public static class GetBudgetHandler
{
    public static async Task<IResult> Handle(
        Guid id,
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
        BudgetId budgetId = new(id);

        Lib.Budget.Entities.Budget? budget = await budgetContext.Budgets
            .Include(b => b.Categories)
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == budgetId, cancellationToken);

        if (budget is null || budget.HouseholdId != userContext.HouseholdId)
        {
            return Results.NotFound();
        }

        return Results.Ok(CreateBudgetHandler.MapToDto(budget));
    }

    private static bool IsFeatureEnabled(IConfiguration configuration) =>
        configuration.GetValue<bool>("Features:Budget", defaultValue: false);
}
