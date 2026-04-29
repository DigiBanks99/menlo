using Menlo.Lib.Budget.Entities;
using Menlo.Lib.Budget.Errors;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Menlo.Api.Budget.Categories;

internal static class CategoryMapper
{
    internal static CategoryDto MapToDto(CategoryNode node, Guid budgetId) =>
        new(
            Id: node.Id.Value,
            BudgetId: budgetId,
            Name: node.Name.Value,
            Description: node.Description,
            ParentId: node.ParentId?.Value,
            CanonicalCategoryId: node.CanonicalCategoryId.Value,
            BudgetFlow: node.BudgetFlow.ToString(),
            Attribution: node.Attribution?.ToString(),
            IncomeContributor: node.IncomeContributor,
            ResponsiblePayer: node.ResponsiblePayer,
            IsDeleted: node.IsDeleted);

    internal static IResult MapError(BudgetError error) => error switch
    {
        DuplicateCategoryNameError => Results.Conflict(new { error.Code, error.Message }),
        CategoryNotFoundError => Results.NotFound(),
        CategoryDepthError e => Results.Problem(detail: e.Message, statusCode: 400, title: "Depth violation"),
        DeletedParentError e => Results.Problem(detail: e.Message, statusCode: 400, title: "Deleted parent"),
        InvalidBudgetDataError e => Results.Problem(detail: e.Message, statusCode: 400, title: "Validation error"),
        _ => Results.Problem(detail: error.Message, statusCode: 400, title: "Bad request"),
    };
}
