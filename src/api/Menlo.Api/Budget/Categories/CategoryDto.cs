namespace Menlo.Api.Budget.Categories;

public sealed record CategoryDto(
    Guid Id,
    Guid BudgetId,
    string Name,
    string? Description,
    Guid? ParentId,
    Guid CanonicalCategoryId,
    string BudgetFlow,
    string? Attribution,
    string? IncomeContributor,
    string? ResponsiblePayer,
    bool IsDeleted);

public sealed record CategoryTreeNode(
    Guid Id,
    string Name,
    string? Description,
    string BudgetFlow,
    string? Attribution,
    string? IncomeContributor,
    string? ResponsiblePayer,
    bool IsDeleted,
    IReadOnlyCollection<CategoryTreeNode> Children);

public sealed record CreateCategoryRequest(
    string Name,
    string BudgetFlow,
    Guid? ParentId = null,
    string? Description = null,
    string? Attribution = null,
    string? IncomeContributor = null,
    string? ResponsiblePayer = null);

public sealed record UpdateCategoryRequest(
    string Name,
    string BudgetFlow,
    string? Description = null,
    string? Attribution = null,
    string? IncomeContributor = null,
    string? ResponsiblePayer = null);

public sealed record ReparentCategoryRequest(Guid? NewParentId = null);
