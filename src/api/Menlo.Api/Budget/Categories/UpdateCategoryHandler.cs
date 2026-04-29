using CSharpFunctionalExtensions;
using Menlo.Application.Budget;
using Menlo.Lib.Auth.Abstractions;
using Menlo.Lib.Auth.Errors;
using Menlo.Lib.Auth.Models;
using Menlo.Lib.Budget.Entities;
using Menlo.Lib.Budget.Enums;
using Menlo.Lib.Budget.Errors;
using Menlo.Lib.Budget.ValueObjects;
using Microsoft.EntityFrameworkCore;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Menlo.Api.Budget.Categories;

public static class UpdateCategoryHandler
{
    public static async Task<IResult> Handle(
        Guid budgetId,
        Guid categoryId,
        UpdateCategoryRequest request,
        IUserContextProvider userContextProvider,
        IBudgetContext budgetContext,
        IConfiguration configuration,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        ILogger logger = loggerFactory.CreateLogger(nameof(UpdateCategoryHandler));

        if (!configuration.GetValue<bool>("Features:Budget", defaultValue: false))
        {
            return Results.NotFound();
        }

        CategoryLog.UpdatingCategory(logger, categoryId, budgetId);

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

        if (!Enum.TryParse<BudgetFlow>(request.BudgetFlow, ignoreCase: true, out BudgetFlow budgetFlow))
        {
            return Results.Problem(
                detail: $"Invalid BudgetFlow value: '{request.BudgetFlow}'. Valid values: {string.Join(", ", Enum.GetNames<BudgetFlow>())}",
                statusCode: 400,
                title: "Validation error");
        }

        Attribution? attribution = null;
        if (request.Attribution is not null)
        {
            if (!Enum.TryParse<Attribution>(request.Attribution, ignoreCase: true, out Attribution parsed))
            {
                return Results.Problem(
                    detail: $"Invalid Attribution value: '{request.Attribution}'. Valid values: {string.Join(", ", Enum.GetNames<Attribution>())}",
                    statusCode: 400,
                    title: "Validation error");
            }

            attribution = parsed;
        }

        BudgetCategoryId catId = new(categoryId);

        Result<CategoryNode, BudgetError> result = budget.UpdateCategory(
            catId,
            request.Name,
            budgetFlow,
            attribution,
            request.Description,
            request.IncomeContributor,
            request.ResponsiblePayer);

        if (result.IsFailure)
        {
            CategoryLog.CategoryOperationFailed(logger, budgetId, result.Error.Code, result.Error.Message);
            return CategoryMapper.MapError(result.Error);
        }

        await budgetContext.SaveChangesAsync(cancellationToken);

        CategoryLog.CategoryUpdated(logger, categoryId, budgetId);

        return Results.Ok(CategoryMapper.MapToDto(result.Value, budgetId));
    }
}
