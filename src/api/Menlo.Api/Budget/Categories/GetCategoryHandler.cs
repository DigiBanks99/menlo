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

public static class GetCategoryHandler
{
    public static async Task<IResult> Handle(
        Guid budgetId,
        Guid categoryId,
        IUserContextProvider userContextProvider,
        IBudgetContext budgetContext,
        IConfiguration configuration,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        ILogger logger = loggerFactory.CreateLogger(nameof(GetCategoryHandler));

        if (!configuration.GetValue<bool>("Features:Budget", defaultValue: false))
        {
            return Results.NotFound();
        }

        CategoryLog.GettingCategory(logger, categoryId, budgetId);

        Result<UserContext, AuthError> userContextResult =
            await userContextProvider.GetUserContextAsync(cancellationToken);

        if (userContextResult.IsFailure)
        {
            return Results.Unauthorized();
        }

        UserContext userContext = userContextResult.Value;
        BudgetId id = new(budgetId);

        Lib.Budget.Entities.Budget? budget = await budgetContext.Budgets
            .Include(b => b.Categories)
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

        if (budget is null || budget.HouseholdId != userContext.HouseholdId)
        {
            return Results.NotFound();
        }

        BudgetCategoryId catId = new(categoryId);
        CategoryNode? node = budget.Categories.FirstOrDefault(c => c.Id == catId);

        if (node is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(CategoryMapper.MapToDto(node, budgetId));
    }
}
