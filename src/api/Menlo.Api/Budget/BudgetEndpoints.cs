using Menlo.Api.Auth.Policies;
using Menlo.Api.Budget.Categories;

namespace Menlo.Api.Budget;

/// <summary>
/// Maps all budget-related endpoints.
/// </summary>
public static class BudgetEndpoints
{
    /// <summary>
    /// Maps budget endpoints to the given API group.
    /// </summary>
    /// <param name="group">The route group builder.</param>
    /// <returns>The route group builder for chaining.</returns>
    public static RouteGroupBuilder MapBudgetEndpoints(this RouteGroupBuilder group)
    {
        RouteGroupBuilder budgets = group
            .MapGroup("/budgets")
            .WithTags("Budgets");

        budgets
            .MapPost("/{year:int}", CreateBudgetHandler.Handle)
            .WithName("CreateBudget")
            .WithSummary("Creates a new budget for the given year, or returns the existing one.")
            .RequireAuthorization(MenloPolicies.CanEditBudget);

        budgets
            .MapGet("/{id:guid}", GetBudgetHandler.Handle)
            .WithName("GetBudget")
            .WithSummary("Retrieves a budget by ID.")
            .RequireAuthorization(MenloPolicies.CanViewBudget);

        budgets
            .MapPost("/{id:guid}/activate", ActivateBudgetHandler.Handle)
            .WithName("ActivateBudget")
            .WithSummary("Activates a budget. Auto-closes the previous year's active budget.")
            .RequireAuthorization(MenloPolicies.CanEditBudget);

        budgets.MapGroup("/{budgetId:guid}/categories")
            .WithTags("Budget Categories")
            .MapCategoryEndpoints();

        return group;
    }
}
