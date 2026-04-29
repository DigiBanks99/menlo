using CSharpFunctionalExtensions;
using Menlo.Application.Budget;
using Menlo.Lib.Auth.Abstractions;
using Menlo.Lib.Auth.Errors;
using Menlo.Lib.Auth.Models;
using Menlo.Lib.Budget.Entities;
using Menlo.Lib.Budget.ValueObjects;
using Microsoft.EntityFrameworkCore;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Menlo.Api.Budget.Categories;

public static class ListCategoriesHandler
{
    public static async Task<IResult> Handle(
        Guid budgetId,
        IUserContextProvider userContextProvider,
        IBudgetContext budgetContext,
        IConfiguration configuration,
        ILoggerFactory loggerFactory,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default)
    {
        ILogger logger = loggerFactory.CreateLogger(nameof(ListCategoriesHandler));

        if (!configuration.GetValue<bool>("Features:Budget", defaultValue: false))
        {
            return Results.NotFound();
        }

        CategoryLog.ListingCategories(logger, budgetId, includeDeleted);

        Result<UserContext, AuthError> userContextResult =
            await userContextProvider.GetUserContextAsync(cancellationToken);

        if (userContextResult.IsFailure)
        {
            return Results.Unauthorized();
        }

        UserContext userContext = userContextResult.Value;
        BudgetId id = new(budgetId);

        IQueryable<Lib.Budget.Entities.Budget> query = includeDeleted
            ? budgetContext.Budgets.IgnoreQueryFilters()
            : budgetContext.Budgets;

        Lib.Budget.Entities.Budget? budget = await query
            .Include(b => b.Categories)
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

        if (budget is null || budget.HouseholdId != userContext.HouseholdId)
        {
            return Results.NotFound();
        }

        IEnumerable<CategoryNode> categories = budget.Categories;
        if (!includeDeleted)
        {
            categories = categories.Where(c => !c.IsDeleted);
        }

        List<CategoryNode> categoryList = categories.ToList();
        ILookup<Guid?, CategoryNode> byParent = categoryList.ToLookup(c => c.ParentId?.Value);

        List<CategoryTreeNode> tree = BuildChildren(byParent, null);

        return Results.Ok(tree);
    }

    private static List<CategoryTreeNode> BuildChildren(ILookup<Guid?, CategoryNode> byParent, Guid? parentId)
    {
        return byParent[parentId]
            .Select(c => new CategoryTreeNode(
                Id: c.Id.Value,
                Name: c.Name.Value,
                Description: c.Description,
                BudgetFlow: c.BudgetFlow.ToString(),
                Attribution: c.Attribution?.ToString(),
                IncomeContributor: c.IncomeContributor,
                ResponsiblePayer: c.ResponsiblePayer,
                IsDeleted: c.IsDeleted,
                Children: BuildChildren(byParent, c.Id.Value)))
            .ToList();
    }
}
