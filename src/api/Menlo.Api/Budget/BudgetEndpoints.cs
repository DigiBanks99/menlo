using Menlo.Api.Auth.Policies;

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

        return group;
    }
}
