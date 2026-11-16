using Menlo.Api.Budget;
using Menlo.Api.Budget.Categories;
using Menlo.Api.Budget.Items;
using Menlo.Api.Budget.Summary;
using Menlo.Api.Ui.Shared;
using Menlo.Application.Budget;
using Menlo.Lib.Auth.Abstractions;
using Menlo.Lib.Common.Abstractions;

namespace Menlo.Api.Ui.Budgets;

/// <summary>
/// The budget operations the UI needs. Each one calls the handler behind the matching JSON endpoint,
/// so pages and the API can't drift apart, and returns an <see cref="Outcome{T}"/> a page can render.
/// </summary>
internal sealed class BudgetUi(
    IUserContextProvider userContextProvider,
    IBudgetContext budgetContext,
    IAuditStampFactory auditStampFactory,
    ISoftDeleteStampFactory softDeleteStampFactory,
    IConfiguration configuration,
    ILoggerFactory loggerFactory)
{
    /// <summary>Whether budget items (line items, summaries) are switched on.</summary>
    public bool ItemsEnabled => configuration.GetValue("Features:BudgetItems", defaultValue: false);

    public async Task<Outcome<List<BudgetListItem>>> ListBudgetsAsync(CancellationToken cancellationToken) =>
        Outcome<List<BudgetListItem>>.From(await ListBudgetsHandler.Handle(
            userContextProvider, budgetContext, configuration, cancellationToken));

    public async Task<Outcome<BudgetDto>> GetBudgetAsync(Guid id, CancellationToken cancellationToken) =>
        Outcome<BudgetDto>.From(await GetBudgetHandler.Handle(
            id, userContextProvider, budgetContext, configuration, cancellationToken));

    public async Task<Outcome<BudgetDto>> CreateOrEnsureBudgetAsync(int year, CancellationToken cancellationToken) =>
        Outcome<BudgetDto>.From(await CreateBudgetHandler.Handle(
            year, userContextProvider, budgetContext, auditStampFactory, configuration, cancellationToken));

    public async Task<Outcome<BudgetSummaryDto>> GetSummaryAsync(Guid budgetId, int? month, CancellationToken cancellationToken) =>
        Outcome<BudgetSummaryDto>.From(await GetBudgetSummaryHandler.Handle(
            budgetId, userContextProvider, budgetContext, configuration, month, cancellationToken));

    public async Task<Outcome<IReadOnlyCollection<CategoryTreeNode>>> ListCategoriesAsync(
        Guid budgetId,
        bool includeDeleted,
        CancellationToken cancellationToken) =>
        Outcome<IReadOnlyCollection<CategoryTreeNode>>.From(await ListCategoriesHandler.Handle(
            budgetId, userContextProvider, budgetContext, configuration, loggerFactory, includeDeleted, cancellationToken));

    public async Task<Outcome<CategoryDto>> GetCategoryAsync(Guid budgetId, Guid categoryId, CancellationToken cancellationToken) =>
        Outcome<CategoryDto>.From(await GetCategoryHandler.Handle(
            budgetId, categoryId, userContextProvider, budgetContext, configuration, loggerFactory, cancellationToken));

    public async Task<Outcome<CategoryDto>> CreateCategoryAsync(
        Guid budgetId,
        CreateCategoryRequest request,
        CancellationToken cancellationToken) =>
        Outcome<CategoryDto>.From(await CreateCategoryHandler.Handle(
            budgetId, request, userContextProvider, budgetContext, auditStampFactory, configuration, loggerFactory, cancellationToken));

    public async Task<Outcome<CategoryDto>> UpdateCategoryAsync(
        Guid budgetId,
        Guid categoryId,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken) =>
        Outcome<CategoryDto>.From(await UpdateCategoryHandler.Handle(
            budgetId, categoryId, request, userContextProvider, budgetContext, configuration, loggerFactory, cancellationToken));

    public async Task<Outcome<object>> DeleteCategoryAsync(Guid budgetId, Guid categoryId, CancellationToken cancellationToken) =>
        Outcome<object>.From(await DeleteCategoryHandler.Handle(
            budgetId, categoryId, userContextProvider, budgetContext, softDeleteStampFactory, configuration, loggerFactory, cancellationToken));

    public async Task<Outcome<object>> RestoreCategoryAsync(Guid budgetId, Guid categoryId, CancellationToken cancellationToken) =>
        Outcome<object>.From(await RestoreCategoryHandler.Handle(
            budgetId, categoryId, userContextProvider, budgetContext, configuration, loggerFactory, cancellationToken));

    public async Task<Outcome<List<BudgetItemDto>>> ListItemsAsync(Guid budgetId, Guid categoryId, CancellationToken cancellationToken) =>
        Outcome<List<BudgetItemDto>>.From(await ListBudgetItemsHandler.Handle(
            budgetId, categoryId, userContextProvider, budgetContext, configuration, month: null, cancellationToken));

    public async Task<Outcome<BudgetItemDto>> CreateItemAsync(
        Guid budgetId,
        Guid categoryId,
        CreateBudgetItemRequest request,
        CancellationToken cancellationToken) =>
        Outcome<BudgetItemDto>.From(await CreateBudgetItemHandler.Handle(
            budgetId, categoryId, request, userContextProvider, budgetContext, configuration, cancellationToken));

    public async Task<Outcome<object>> BulkCreateItemsAsync(
        Guid budgetId,
        Guid categoryId,
        BulkCreateBudgetItemRequest request,
        CancellationToken cancellationToken) =>
        Outcome<object>.From(await BulkCreateBudgetItemHandler.Handle(
            budgetId, categoryId, request, userContextProvider, budgetContext, configuration, cancellationToken));

    public async Task<Outcome<object>> FillForwardAsync(
        Guid budgetId,
        Guid categoryId,
        FillForwardBudgetItemRequest request,
        CancellationToken cancellationToken) =>
        Outcome<object>.From(await FillForwardHandler.Handle(
            budgetId, categoryId, request, userContextProvider, budgetContext, configuration, cancellationToken));

    public async Task<Outcome<BudgetItemDto>> UpdateItemAsync(
        Guid budgetId,
        Guid categoryId,
        Guid itemId,
        UpdateBudgetItemRequest request,
        CancellationToken cancellationToken) =>
        Outcome<BudgetItemDto>.From(await UpdateBudgetItemHandler.Handle(
            budgetId, categoryId, itemId, request, userContextProvider, budgetContext, configuration, cancellationToken));

    public async Task<Outcome<BudgetItemDto>> RealizeItemAsync(
        Guid budgetId,
        Guid categoryId,
        Guid itemId,
        decimal amount,
        CancellationToken cancellationToken) =>
        Outcome<BudgetItemDto>.From(await RealizeItemHandler.Handle(
            budgetId, categoryId, itemId, new RealizeBudgetItemRequest(amount), userContextProvider, budgetContext, configuration, cancellationToken));

    public async Task<Outcome<BudgetItemDto>> RecordItemSpentAsync(
        Guid budgetId,
        Guid categoryId,
        Guid itemId,
        decimal amount,
        CancellationToken cancellationToken) =>
        Outcome<BudgetItemDto>.From(await RecordItemSpentHandler.Handle(
            budgetId, categoryId, itemId, new RecordBudgetItemSpentRequest(amount), userContextProvider, budgetContext, configuration, cancellationToken));

    public async Task<Outcome<object>> DeleteItemAsync(Guid budgetId, Guid categoryId, Guid itemId, CancellationToken cancellationToken) =>
        Outcome<object>.From(await DeleteBudgetItemHandler.Handle(
            budgetId, categoryId, itemId, userContextProvider, budgetContext, softDeleteStampFactory, configuration, cancellationToken));
}
