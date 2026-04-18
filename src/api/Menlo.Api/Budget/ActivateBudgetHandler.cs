using CSharpFunctionalExtensions;
using Menlo.Application.Budget;
using Menlo.Lib.Auth.Abstractions;
using Menlo.Lib.Auth.Errors;
using Menlo.Lib.Auth.Models;
using Menlo.Lib.Budget.Enums;
using Menlo.Lib.Budget.ValueObjects;
using Menlo.Lib.Common.Abstractions;
using Microsoft.EntityFrameworkCore;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Menlo.Api.Budget;

/// <summary>
/// Handler for the POST /api/budgets/{id}/activate endpoint.
/// Activates a Draft budget and auto-closes the previous year's Active budget.
/// </summary>
public static class ActivateBudgetHandler
{
    public static async Task<IResult> Handle(
        Guid id,
        IUserContextProvider userContextProvider,
        IBudgetContext budgetContext,
        IAuditStampFactory auditStampFactory,
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
            .FirstOrDefaultAsync(b => b.Id == budgetId, cancellationToken);

        if (budget is null || budget.HouseholdId != userContext.HouseholdId)
        {
            return Results.NotFound();
        }

        UnitResult<Lib.Budget.Errors.BudgetError> activateResult = budget.Activate();
        if (activateResult.IsFailure)
        {
            return Results.Problem(
                detail: activateResult.Error.Message,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Budget activation failed");
        }

        // Auto-close any Active budget from the previous year for the same household
        Lib.Budget.Entities.Budget? previousYearBudget = await budgetContext.Budgets
            .FirstOrDefaultAsync(
                b => b.HouseholdId == userContext.HouseholdId
                    && b.Year == budget.Year - 1
                    && b.Status == BudgetStatus.Active,
                cancellationToken);

        if (previousYearBudget is not null)
        {
            previousYearBudget.Close();
        }

        await budgetContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(CreateBudgetHandler.MapToDto(budget));
    }

    private static bool IsFeatureEnabled(IConfiguration configuration) =>
        configuration.GetValue<bool>("Features:Budget", defaultValue: false);
}
