using System.Diagnostics.CodeAnalysis;

namespace Menlo.Api.Budget.Categories;

[SuppressMessage("Usage", "CA2254:Template should be a static expression", Justification = "LoggerMessage source generator")]
internal static partial class CategoryLog
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Creating category '{CategoryName}' in budget {BudgetId}")]
    public static partial void CreatingCategory(ILogger logger, string categoryName, Guid budgetId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Created category {CategoryId} in budget {BudgetId}")]
    public static partial void CategoryCreated(ILogger logger, Guid categoryId, Guid budgetId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Listing categories for budget {BudgetId} (includeDeleted={IncludeDeleted})")]
    public static partial void ListingCategories(ILogger logger, Guid budgetId, bool includeDeleted);

    [LoggerMessage(Level = LogLevel.Information, Message = "Getting category {CategoryId} from budget {BudgetId}")]
    public static partial void GettingCategory(ILogger logger, Guid categoryId, Guid budgetId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Updating category {CategoryId} in budget {BudgetId}")]
    public static partial void UpdatingCategory(ILogger logger, Guid categoryId, Guid budgetId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Category {CategoryId} updated in budget {BudgetId}")]
    public static partial void CategoryUpdated(ILogger logger, Guid categoryId, Guid budgetId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Reparenting category {CategoryId} in budget {BudgetId}")]
    public static partial void ReparentingCategory(ILogger logger, Guid categoryId, Guid budgetId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Category {CategoryId} reparented in budget {BudgetId}")]
    public static partial void CategoryReparented(ILogger logger, Guid categoryId, Guid budgetId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Soft-deleting category {CategoryId} in budget {BudgetId}")]
    public static partial void DeletingCategory(ILogger logger, Guid categoryId, Guid budgetId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Category {CategoryId} soft-deleted in budget {BudgetId}")]
    public static partial void CategoryDeleted(ILogger logger, Guid categoryId, Guid budgetId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Restoring category {CategoryId} in budget {BudgetId}")]
    public static partial void RestoringCategory(ILogger logger, Guid categoryId, Guid budgetId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Category {CategoryId} restored in budget {BudgetId}")]
    public static partial void CategoryRestored(ILogger logger, Guid categoryId, Guid budgetId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Category operation failed for budget {BudgetId}: {ErrorCode} - {ErrorMessage}")]
    public static partial void CategoryOperationFailed(ILogger logger, Guid budgetId, string errorCode, string errorMessage);
}
