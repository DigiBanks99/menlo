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

        group
            .MapPut("/{itemId:guid}", UpdateBudgetItemHandler.Handle)
            .WithName("UpdateBudgetItem")
            .WithSummary("Updates an existing budget item's amounts and/or splits.")
            .RequireAuthorization(MenloPolicies.CanEditBudget);

        group
            .MapPut("/{itemId:guid}/realize", RealizeItemHandler.Handle)
            .WithName("RealizeItem")
            .WithSummary("Records the realized amount for a budget item (bill/payslip arrived).")
            .RequireAuthorization(MenloPolicies.CanEditBudget);

        group
            .MapPut("/{itemId:guid}/spent", RecordItemSpentHandler.Handle)
            .WithName("RecordItemSpent")
            .WithSummary("Records the spent amount for a budget item (payment made).")
            .RequireAuthorization(MenloPolicies.CanEditBudget);

        return group;
    }
}
