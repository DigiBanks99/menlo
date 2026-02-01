namespace Menlo.Lib.Budget.Endpoints;

using System.Security.Claims;
using CSharpFunctionalExtensions;
using Menlo.Api.Auth.Policies;
using Menlo.Api.Persistence.Data;
using Menlo.Lib.Budget.Entities;
using Menlo.Lib.Budget.Errors;
using Menlo.Lib.Budget.ValueObjects;
using Menlo.Lib.Common.ValueObjects;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Endpoint for deleting budget categories.
/// </summary>
public static class DeleteCategoryEndpoint
{
    extension (RouteGroupBuilder group)
    {
        public RouteGroupBuilder MapDeleteCategory()
        {
            group.MapDelete("{id:guid}/categories/{categoryId:guid}", Handle)
                .WithName("DeleteCategory")
                .WithSummary("Deletes a budget category")
                .RequireAuthorization(MenloPolicies.CanEditBudget)
                .Produces(StatusCodes.Status204NoContent)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status403Forbidden);

            return group;
        }
    }

    private static async Task<Results<NoContent, NotFound<ProblemDetails>, BadRequest<ProblemDetails>>> Handle(
        Guid id,
        Guid categoryId,
        ClaimsPrincipal user,
        MenloDbContext dbContext,
        CancellationToken cancellationToken)
    {
        // Resolve current user ID from claims
        UserId userId = GetUserIdFromClaims(user);

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

        // Remove category
        Result<bool, BudgetError> result = budget.RemoveCategory(categoryIdValue);

        if (result.IsFailure)
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Category deletion failed",
                Detail = result.Error.Message,
                Extensions = { ["errorCode"] = result.Error.Code }
            });
        }

        // Save changes
        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
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