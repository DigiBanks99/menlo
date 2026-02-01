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
/// Endpoint for creating a new budget.
/// </summary>
public static class CreateBudgetEndpoint
{
    extension (RouteGroupBuilder group)
    {
        public RouteGroupBuilder MapCreateBudget()
        {
            group.MapPost("", Handle)
                .WithName("CreateBudget")
                .WithSummary("Creates a new budget")
                .RequireAuthorization(MenloPolicies.CanEditBudget)
                .Produces<BudgetResponse>(StatusCodes.Status201Created)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status403Forbidden);

            return group;
        }
    }

    private static async Task<Results<Created<BudgetResponse>, BadRequest<ProblemDetails>, Conflict<ProblemDetails>>> Handle(
        [FromBody] CreateBudgetRequest request,
        ClaimsPrincipal user,
        MenloDbContext dbContext,
        LinkGenerator linkGenerator,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        // Resolve current user ID from claims
        UserId userId = GetUserIdFromClaims(user);

        // Create budget period
        Result<BudgetPeriod, BudgetError> periodResult = BudgetPeriod.Create(request.Year, request.Month);
        if (periodResult.IsFailure)
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid budget period",
                Detail = periodResult.Error.Message,
                Extensions = { ["errorCode"] = periodResult.Error.Code }
            });
        }

        // Check for duplicate budget (same user, period, and name)
        bool exists = await dbContext.Budgets
            .AnyAsync(
                b => b.OwnerId == userId
                    && b.Period == periodResult.Value
                    && b.Name == request.Name.Trim(),
                cancellationToken);

        if (exists)
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Duplicate budget",
                Detail = $"A budget named '{request.Name}' already exists for {periodResult.Value.Month}/{periodResult.Value.Year}.",
                Extensions = { ["errorCode"] = "BUDGET_DUPLICATE" }
            });
        }

        // Create budget aggregate
        Result<Entities.Budget, BudgetError> budgetResult = Entities.Budget.Create(
            userId,
            request.Name,
            periodResult.Value,
            request.Currency);

        if (budgetResult.IsFailure)
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Budget creation failed",
                Detail = budgetResult.Error.Message,
                Extensions = { ["errorCode"] = budgetResult.Error.Code }
            });
        }

        // Add to database
        dbContext.Budgets.Add(budgetResult.Value);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Build response
        BudgetResponse response = MapToBudgetResponse(budgetResult.Value);

        // Generate location URL
        string? locationUrl = linkGenerator.GetPathByName(
            httpContext,
            "GetBudget",
            new { id = budgetResult.Value.Id.Value });

        return TypedResults.Created(locationUrl ?? $"/api/budgets/{budgetResult.Value.Id.Value}", response);
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
