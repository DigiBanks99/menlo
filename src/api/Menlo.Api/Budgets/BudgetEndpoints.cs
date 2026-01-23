namespace Menlo.Lib.Budget;

using Menlo.Lib.Budget.Endpoints;

/// <summary>
/// Extension methods for registering budget endpoints.
/// </summary>
public static class BudgetEndpoints
{
    /// <summary>
    /// Maps budget endpoints to the application.
    /// </summary>
    public static IEndpointRouteBuilder MapBudgetEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGroup("/budgets")
            .WithTags("Budgets")
            .MapCreateBudget()
            .MapGetBudget()
            .MapListBudgets()
            .MapUpdateBudget()
            .MapActivateBudget()
            .MapCreateCategory()
            .MapUpdateCategory()
            .MapDeleteCategory()
            .MapSetPlannedAmount()
            .MapClearPlannedAmount();

        return app;
    }
}
