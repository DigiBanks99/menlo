using CSharpFunctionalExtensions;
using Menlo.Application.Budget;
using Menlo.Lib.Auth.Abstractions;
using Menlo.Lib.Auth.Errors;
using Menlo.Lib.Auth.Models;
using Menlo.Lib.Budget.Entities;
using Menlo.Lib.Budget.Enums;
using Menlo.Lib.Budget.Errors;
using Menlo.Lib.Budget.ValueObjects;
using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Common.Enums;
using Microsoft.EntityFrameworkCore;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Menlo.Api.Budget.Categories;

public static class CreateCategoryHandler
{
    public static async Task<IResult> Handle(
        Guid budgetId,
        CreateCategoryRequest request,
        IUserContextProvider userContextProvider,
        IBudgetContext budgetContext,
        IAuditStampFactory auditStampFactory,
        IConfiguration configuration,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        ILogger logger = loggerFactory.CreateLogger(nameof(CreateCategoryHandler));
        if (!configuration.GetValue<bool>("Features:Budget", defaultValue: false))
        {
            return Results.NotFound();
        }

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

        BudgetCategoryId? parentId = request.ParentId.HasValue ? new BudgetCategoryId(request.ParentId.Value) : null;

        CategoryLog.CreatingCategory(logger, request.Name, budgetId);

        Result<CategoryNode, BudgetError> result = budget.AddCategory(
            request.Name,
            budgetFlow,
            parentId,
            request.Description,
            attribution,
            request.IncomeContributor,
            request.ResponsiblePayer);

        if (result.IsFailure)
        {
            CategoryLog.CategoryOperationFailed(logger, budgetId, result.Error.Code, result.Error.Message);
            return CategoryMapper.MapError(result.Error);
        }

        CategoryNode node = result.Value;

        var canonicalCategory = CanonicalCategory.Create(node.CanonicalCategoryId, node.Name.Value);
        canonicalCategory.Audit(auditStampFactory, AuditOperation.Create);
        budgetContext.CanonicalCategories.Add(canonicalCategory);

        await budgetContext.SaveChangesAsync(cancellationToken);

        CategoryLog.CategoryCreated(logger, node.Id.Value, budgetId);

        CategoryDto dto = CategoryMapper.MapToDto(node, budgetId);
        return Results.Created($"/api/budgets/{budgetId}/categories/{dto.Id}", dto);
    }
}
