namespace Menlo.Lib.Budget.Endpoints;

using System.Security.Claims;
using CSharpFunctionalExtensions;
using Menlo.Api.Auth.Policies;
using Menlo.Api.Persistence.Data;
using Menlo.Lib.Budget.Entities;
using Menlo.Lib.Budget.Errors;
using Menlo.Lib.Budget.Models;
using Menlo.Lib.Budget.ValueObjects;
using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Common.ValueObjects;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Endpoint for setting planned amounts on budget categories.
/// </summary>
public static class SetPlannedAmountEndpoint
{
    extension (RouteGroupBuilder group)
    {
        public RouteGroupBuilder MapSetPlannedAmount()
        {
            group.MapPut("{id:guid}/categories/{categoryId:guid}/planned-amount", Handle)
                .WithName("SetPlannedAmount")
                .WithSummary("Sets the planned amount for a budget category")
                .RequireAuthorization(MenloPolicies.CanEditBudget)
                .Produces<BudgetCategoryResponse>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status403Forbidden);

            return group;
        }
    }

    private static async Task<Results<Ok<BudgetCategoryResponse>, NotFound<ProblemDetails>, BadRequest<ProblemDetails>>> Handle(
        Guid id,
        Guid categoryId,
        SetPlannedAmountRequest request,
        ClaimsPrincipal user,
        MenloDbContext dbContext,
        CancellationToken cancellationToken)
    {
        // Resolve current user ID from claims
        UserId userId = GetUserIdFromClaims(user);

        // Validate request
        if (request.Amount < 0)
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation failed",
                Detail = "Amount cannot be negative",
                Extensions = { ["errorCode"] = "VALIDATION_FAILED" }
            });
        }

        if (string.IsNullOrWhiteSpace(request.Currency))
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation failed",
                Detail = "Currency is required",
                Extensions = { ["errorCode"] = "VALIDATION_FAILED" }
            });
        }

        // Create Money value object
        Result<Money, Error> moneyResult = Money.Create(request.Amount, request.Currency);
        if (moneyResult.IsFailure)
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid amount",
                Detail = moneyResult.Error.Message,
                Extensions = { ["errorCode"] = "INVALID_AMOUNT" }
            });
        }

        // Query budget with categories
        BudgetId budgetId = new(id);
        Budget? budget = await dbContext.Budgets
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

        // Validate currency matches budget currency
        if (!string.Equals(moneyResult.Value.Currency, budget.Currency, StringComparison.OrdinalIgnoreCase))
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Currency mismatch",
                Detail = $"Amount currency '{moneyResult.Value.Currency}' does not match budget currency '{budget.Currency}'",
                Extensions = { ["errorCode"] = "CURRENCY_MISMATCH" }
            });
        }

        // Check if category exists
        BudgetCategoryId categoryIdValue = new(categoryId);
        BudgetCategory? category = budget.FindCategory(categoryIdValue);
        
        if (category is null)
        {
            return TypedResults.NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Category not found",
                Detail = "Category not found in this budget",
                Extensions = { ["errorCode"] = "CATEGORY_NOT_FOUND" }
            });
        }

        // Set planned amount
        Result<bool, BudgetError> result = budget.SetPlannedAmount(categoryIdValue, moneyResult.Value);

        if (result.IsFailure)
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Failed to set planned amount",
                Detail = result.Error.Message,
                Extensions = { ["errorCode"] = result.Error.Code }
            });
        }

        // Save changes
        await dbContext.SaveChangesAsync(cancellationToken);

        // Build response - reload category to get updated values
        category = budget.FindCategory(categoryIdValue)!;
        BudgetCategoryResponse response = MapToCategoryResponse(category);

        return TypedResults.Ok(response);
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