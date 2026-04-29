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

public static class ReparentCategoryHandler
{
    public static async Task<IResult> Handle(
        Guid budgetId,
        Guid categoryId,
        ReparentCategoryRequest request,
        IUserContextProvider userContextProvider,
        IBudgetContext budgetContext,
        IConfiguration configuration,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        ILogger logger = loggerFactory.CreateLogger(nameof(ReparentCategoryHandler));

        if (!configuration.GetValue<bool>("Features:Budget", defaultValue: false))
        {
            return Results.NotFound();
        }

        CategoryLog.ReparentingCategory(logger, categoryId, budgetId);

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
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

        if (budget is null || budget.HouseholdId != userContext.HouseholdId)
        {
            return Results.NotFound();
        }

        BudgetCategoryId catId = new(categoryId);
        BudgetCategoryId? newParentId = request.NewParentId.HasValue
            ? new BudgetCategoryId(request.NewParentId.Value)
            : null;

        Result<CategoryNode, BudgetError> result = budget.ReparentCategory(catId, newParentId);

        if (result.IsFailure)
        {
            CategoryLog.CategoryOperationFailed(logger, budgetId, result.Error.Code, result.Error.Message);
            return CategoryMapper.MapError(result.Error);
        }

        await budgetContext.SaveChangesAsync(cancellationToken);

        CategoryLog.CategoryReparented(logger, categoryId, budgetId);

        return Results.Ok(CategoryMapper.MapToDto(result.Value, budgetId));
    }
}
