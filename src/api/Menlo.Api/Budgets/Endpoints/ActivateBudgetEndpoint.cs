namespace Menlo.Lib.Budget.Endpoints;

using System.Security.Claims;
using CSharpFunctionalExtensions;
using Menlo.Api.Auth.Policies;
using Menlo.Api.Persistence.Data;
using Menlo.Lib.Budget;
using Menlo.Lib.Budget.Entities;
using Menlo.Lib.Budget.Errors;
using Menlo.Lib.Budget.Models;
using Menlo.Lib.Budget.ValueObjects;
using Menlo.Lib.Common;
using Menlo.Lib.Common.ValueObjects;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Endpoint for activating a budget.
/// </summary>
public static class ActivateBudgetEndpoint
{
    extension (RouteGroupBuilder group)
    {
        public RouteGroupBuilder MapActivateBudget()
        {
            group.MapPost("{id:guid}/activate", Handle)
                .WithName("ActivateBudget")
                .WithSummary("Activates a budget")
                .RequireAuthorization(MenloPolicies.CanEditBudget)
                .Produces<BudgetResponse>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status403Forbidden);

            return group;
        }
    }

    private static async Task<Results<Ok<BudgetResponse>, NotFound<ProblemDetails>, BadRequest<ProblemDetails>>> Handle(
        Guid id,
        ClaimsPrincipal user,
        MenloDbContext dbContext,
        CancellationToken cancellationToken)
    {
        // Resolve current user ID from claims
        UserId userId = GetUserIdFromClaims(user);

        // Query budget with categories
        BudgetId budgetId = new(id);
        Entities.Budget? budget = await dbContext.Budgets
            .Include(b => b.Categories)
            .ThenInclude(c => c.Children)
            .FirstOrDefaultAsync(
                b => b.Id == budgetId && b.OwnerId == userId,
                cancellationToken);

        if (budget is null)
        {
            return TypedResults.NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Budget not found",
                Detail = "Budget not found",
                Extensions = { ["errorCode"] = "BUDGET_NOT_FOUND" }
            });
        }

        // Activate the budget
        Result<bool, BudgetError> activateResult = budget.Activate();

        if (activateResult.IsFailure)
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Budget activation failed",
                Detail = activateResult.Error.Message,
                Extensions = { ["errorCode"] = activateResult.Error.Code }
            });
        }

        // Save changes
        await dbContext.SaveChangesAsync(cancellationToken);

        // Build response
        BudgetResponse response = MapToBudgetResponse(budget);

        return TypedResults.Ok(response);
    }

    private static BudgetResponse MapToBudgetResponse(Entities.Budget budget)
    {
        Money total = budget.GetTotal();
        return new BudgetResponse(
            Id: budget.Id.Value,
            Name: budget.Name,
            Year: budget.Period.Year,
            Month: budget.Period.Month,
            Currency: budget.Currency,
            Status: budget.Status.ToString(),
            Categories: budget.Categories.Select(MapToCategoryResponse).ToList(),
            Total: new MoneyResponse(total.Amount, total.Currency),
            CreatedAt: budget.CreatedAt,
            ModifiedAt: budget.ModifiedAt);
    }

    private static BudgetCategoryResponse MapToCategoryResponse(BudgetCategory category)
    {
        return new BudgetCategoryResponse(
            Id: category.Id.Value,
            Name: category.Name,
            Description: category.Description,
            ParentId: category.ParentId?.Value,
            PlannedAmount: category.PlannedAmount is { } amount
                ? new MoneyResponse(amount.Amount, amount.Currency)
                : null,
            DisplayOrder: category.DisplayOrder,
            IsRoot: category.IsRoot,
            IsLeaf: category.IsLeaf,
            Children: category.Children.Select(MapToCategoryResponse).ToList());
    }

    private static UserId GetUserIdFromClaims(ClaimsPrincipal user)
    {
        // Try to get the user ID from the 'oid' claim (Azure AD object ID) or NameIdentifier
        string? oidClaim = user.FindFirst("oid")?.Value
            ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (Guid.TryParse(oidClaim, out Guid userId))
        {
            return new UserId(userId);
        }

        // Fallback to empty GUID if no valid ID found
        return new UserId(Guid.Empty);
    }
}
