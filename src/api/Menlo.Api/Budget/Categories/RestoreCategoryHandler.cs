using CSharpFunctionalExtensions;
using Menlo.Application.Budget;
using Menlo.Lib.Auth.Abstractions;
using Menlo.Lib.Auth.Errors;
using Menlo.Lib.Auth.Models;
using Menlo.Lib.Budget.Entities;
using Menlo.Lib.Budget.Errors;
using Menlo.Lib.Budget.ValueObjects;
using Microsoft.EntityFrameworkCore;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Menlo.Api.Budget.Categories;

public static class RestoreCategoryHandler
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
        ILogger logger = loggerFactory.CreateLogger(nameof(RestoreCategoryHandler));

        if (!configuration.GetValue<bool>("Features:Budget", defaultValue: false))
        {
            return Results.NotFound();
        }

        CategoryLog.RestoringCategory(logger, categoryId, budgetId);

        Result<UserContext, AuthError> userContextResult =
            await userContextProvider.GetUserContextAsync(cancellationToken);

        if (userContextResult.IsFailure)
        {
            return Results.Unauthorized();
        }

        UserContext userContext = userContextResult.Value;
        BudgetId id = new(budgetId);

        Lib.Budget.Entities.Budget? budget = await budgetContext.Budgets
            .IgnoreQueryFilters()
            .Include(b => b.Categories)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

        if (budget is null || budget.HouseholdId != userContext.HouseholdId)
        {
            return Results.NotFound();
        }

        BudgetCategoryId catId = new(categoryId);

        UnitResult<BudgetError> result = budget.RestoreCategory(catId);

        if (result.IsFailure)
        {
            CategoryLog.CategoryOperationFailed(logger, budgetId, result.Error.Code, result.Error.Message);
            return CategoryMapper.MapError(result.Error);
        }

        await budgetContext.SaveChangesAsync(cancellationToken);

        CategoryNode? node = budget.Categories.FirstOrDefault(c => c.Id == catId);

        CategoryLog.CategoryRestored(logger, categoryId, budgetId);

        return Results.Ok(CategoryMapper.MapToDto(node!, budgetId));
    }
}
