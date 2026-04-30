using Menlo.Api.Auth.Policies;

namespace Menlo.Api.Budget.Items;

public static class BudgetItemEndpoints
{
    public static RouteGroupBuilder MapBudgetItemEndpoints(this RouteGroupBuilder group)
    {
        group
            .MapPost("/", CreateBudgetItemHandler.Handle)
            .WithName("CreateBudgetItem")
            .WithSummary("Creates a new budget item for a category in a specific month.")
            .RequireAuthorization(MenloPolicies.CanEditBudget);

        group
            .MapGet("/", ListBudgetItemsHandler.Handle)
            .WithName("ListBudgetItems")
            .WithSummary("Lists budget items for a category, optionally filtered by month.")
            .RequireAuthorization(MenloPolicies.CanViewBudget);

        return group;
    }
}
