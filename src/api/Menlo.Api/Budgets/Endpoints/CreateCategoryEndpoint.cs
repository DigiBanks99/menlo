namespace Menlo.Lib.Budget.Endpoints;

using System.Security.Claims;
using CSharpFunctionalExtensions;
using Menlo.Api.Auth.Policies;
using Menlo.Api.Persistence.Data;
using Menlo.Lib.Budget.Entities;
using Menlo.Lib.Budget.Errors;
using Menlo.Lib.Budget.Models;
using Menlo.Lib.Budget.ValueObjects;
using Menlo.Lib.Common.ValueObjects;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Endpoint for creating budget categories.
/// </summary>
public static class CreateCategoryEndpoint
{
    extension (RouteGroupBuilder group)
    {
        public RouteGroupBuilder MapCreateCategory()
        {
            group.MapPost("{id:guid}/categories", Handle)
                .WithName("CreateCategory")
                .WithSummary("Creates a new budget category")
                .RequireAuthorization(MenloPolicies.CanEditBudget)
                .Produces<BudgetCategoryResponse>(StatusCodes.Status201Created)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status403Forbidden);

            return group;
        }
    }

    private static async Task<Results<Created<BudgetCategoryResponse>, NotFound<ProblemDetails>, BadRequest<ProblemDetails>, Conflict<ProblemDetails>>> Handle(
        Guid id,
        CreateCategoryRequest request,
        ClaimsPrincipal user,
        MenloDbContext dbContext,
        CancellationToken cancellationToken)
    {
        // Resolve current user ID from claims
        UserId userId = GetUserIdFromClaims(user);

        // Validate request
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation failed",
                Detail = "Category name is required",
                Extensions = { ["errorCode"] = "VALIDATION_FAILED" }
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

        // Create category (root or subcategory)
        Result<BudgetCategory, BudgetError> result;
        
        if (request.ParentId.HasValue)
        {
            // Creating a subcategory
            BudgetCategoryId parentId = new(request.ParentId.Value);
            result = budget.AddSubcategory(parentId, request.Name, request.Description);
        }
        else
        {
            // Creating a root category
            result = budget.AddCategory(request.Name, request.Description);
        }

        if (result.IsFailure)
        {
            return result.Error.Code switch
            {
                "Budget.DuplicateCategory" => TypedResults.Conflict(new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Category already exists",
                    Detail = result.Error.Message,
                    Extensions = { ["errorCode"] = result.Error.Code }
                }),
                _ => TypedResults.BadRequest(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Category creation failed",
                    Detail = result.Error.Message,
                    Extensions = { ["errorCode"] = result.Error.Code }
                })
            };
        }

        // Save changes
        await dbContext.SaveChangesAsync(cancellationToken);

        // Build response
        BudgetCategory category = result.Value;
        BudgetCategoryResponse response = MapToCategoryResponse(category);

        return TypedResults.Created($"/api/budgets/{id}/categories/{category.Id.Value}", response);
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