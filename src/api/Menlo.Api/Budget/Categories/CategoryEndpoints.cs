using Menlo.Api.Auth.Policies;

namespace Menlo.Api.Budget.Categories;

public static class CategoryEndpoints
{
    public static RouteGroupBuilder MapCategoryEndpoints(this RouteGroupBuilder group)
    {
        group
            .MapPost("/", CreateCategoryHandler.Handle)
            .WithName("CreateCategory")
            .WithSummary("Creates a new category in the budget.")
            .RequireAuthorization(MenloPolicies.CanEditBudget);

        group
            .MapGet("/", ListCategoriesHandler.Handle)
            .WithName("ListCategories")
            .WithSummary("Lists categories as a tree structure.")
            .RequireAuthorization(MenloPolicies.CanViewBudget);

        group
            .MapGet("/{categoryId:guid}", GetCategoryHandler.Handle)
            .WithName("GetCategory")
            .WithSummary("Gets a category by ID.")
            .RequireAuthorization(MenloPolicies.CanViewBudget);

        group
            .MapPut("/{categoryId:guid}", UpdateCategoryHandler.Handle)
            .WithName("UpdateCategory")
            .WithSummary("Updates a category's properties.")
            .RequireAuthorization(MenloPolicies.CanEditBudget);

        group
            .MapPut("/{categoryId:guid}/reparent", ReparentCategoryHandler.Handle)
            .WithName("ReparentCategory")
            .WithSummary("Moves a category to a new parent.")
            .RequireAuthorization(MenloPolicies.CanEditBudget);

        group
            .MapDelete("/{categoryId:guid}", DeleteCategoryHandler.Handle)
            .WithName("DeleteCategory")
            .WithSummary("Soft-deletes a category and its children.")
            .RequireAuthorization(MenloPolicies.CanEditBudget);

        group
            .MapPut("/{categoryId:guid}/restore", RestoreCategoryHandler.Handle)
            .WithName("RestoreCategory")
            .WithSummary("Restores a soft-deleted category and its children.")
            .RequireAuthorization(MenloPolicies.CanEditBudget);

        return group;
    }
}
