namespace Menlo.Lib.Budget.Endpoints;

using System.Security.Claims;
using Menlo.Api.Auth.Policies;
using Menlo.Api.Persistence.Data;
using Menlo.Lib.Budget.Enums;
using Menlo.Lib.Budget.Models;
using Menlo.Lib.Common;
using Menlo.Lib.Common.ValueObjects;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Endpoint for listing budgets for the current user.
/// </summary>
public static class ListBudgetsEndpoint
{
    extension (RouteGroupBuilder group)
    {
        public RouteGroupBuilder MapListBudgets()
        {
            group.MapGet("", Handle)
                .WithName("ListBudgets")
                .WithSummary("Lists budgets for the current user")
                .RequireAuthorization(MenloPolicies.CanViewBudget)
                .Produces<IReadOnlyList<BudgetSummaryResponse>>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status403Forbidden);

            return group;
        }
    }

    private static async Task<Ok<IReadOnlyList<BudgetSummaryResponse>>> Handle(
        [FromQuery] int? year,
        [FromQuery] string? status,
        ClaimsPrincipal user,
        MenloDbContext dbContext,
        CancellationToken cancellationToken)
    {
        // Resolve current user ID from claims
        UserId userId = GetUserIdFromClaims(user);

        // Build query
        IQueryable<Entities.Budget> query = dbContext.Budgets
            .Include(b => b.Categories)
            .Where(b => b.OwnerId == userId);

        // Apply filters
        if (year.HasValue)
        {
            query = query.Where(b => b.Period.Year == year.Value);
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<BudgetStatus>(status, ignoreCase: true, out BudgetStatus parsedStatus))
        {
            query = query.Where(b => b.Status == parsedStatus);
        }

        // Execute query and order by period descending (most recent first)
        List<Entities.Budget> budgets = await query
            .OrderByDescending(b => b.Period.Year)
            .ThenByDescending(b => b.Period.Month)
            .ThenBy(b => b.Name)
            .ToListAsync(cancellationToken);

        // Map to summary responses
        List<BudgetSummaryResponse> responses = budgets
            .Select(MapToSummaryResponse)
            .ToList();

        return TypedResults.Ok<IReadOnlyList<BudgetSummaryResponse>>(responses);
    }

    private static BudgetSummaryResponse MapToSummaryResponse(Entities.Budget budget)
    {
        Money total = budget.GetTotal();
        int categoryCount = budget.GetAllCategories().Count();

        return new BudgetSummaryResponse(
            Id: budget.Id.Value,
            Name: budget.Name,
            Year: budget.Period.Year,
            Month: budget.Period.Month,
            Currency: budget.Currency,
            Status: budget.Status.ToString(),
            Total: new MoneyResponse(total.Amount, total.Currency),
            CategoryCount: categoryCount,
            CreatedAt: budget.CreatedAt);
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
