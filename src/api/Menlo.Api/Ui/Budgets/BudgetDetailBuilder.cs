using Menlo.Api.Budget;
using Menlo.Api.Budget.Categories;
using Menlo.Api.Budget.Items;
using Menlo.Api.Budget.Summary;
using Menlo.Api.Ui.Shared;
using Menlo.Ui.Components;
using Menlo.Ui.Utilities;

namespace Menlo.Api.Ui.Budgets;

/// <summary>
/// Builds the budget detail page's view model from the budget, its categories, the open category's
/// items and the summary. All the page's decisions are made here: what is a leaf, what matches the
/// filters, which links go where, how figures are formatted.
/// </summary>
internal static class BudgetDetailBuilder
{
    private const string Currency = "ZAR";

    public static async Task<BudgetDetailPageModel> BuildAsync(
        BudgetUi budgets,
        BudgetDto budget,
        BudgetDetailQuery query,
        bool canEdit,
        int currentYear,
        CancellationToken cancellationToken)
    {
        Outcome<IReadOnlyCollection<CategoryTreeNode>> categories =
            await budgets.ListCategoriesAsync(budget.Id, query.Deleted, cancellationToken);

        CategoryNodeDto? selected = query.Category is null
            ? null
            : budget.Categories.FirstOrDefault(category => category.Id == query.Category && IsLeaf(category.Id, budget.Categories));

        // An unknown or non-leaf category in the URL is ignored
        BudgetDetailQuery state = selected is null ? query with { Category = null } : query;

        ItemsWorkspaceModel? workspace = selected is null
            ? null
            : await BuildWorkspaceAsync(budgets, budget.Id, selected, canEdit, cancellationToken);

        Outcome<BudgetSummaryDto> summary =
            await budgets.GetSummaryAsync(budget.Id, state.IsMonthly ? state.Month : null, cancellationToken);

        return new BudgetDetailPageModel(
            budget.Id,
            budget.Year,
            budget.Status,
            BudgetPresentation.StatusTone(budget.Status),
            MoneyFormat.Format(budget.TotalPlannedMonthlyAmount.Amount, budget.TotalPlannedMonthlyAmount.Currency),
            NextYearToCreate: budget.Year == currentYear ? currentYear + 1 : null,
            canEdit,
            BuildTree(budget.Id, state, categories, canEdit),
            BuildLeaves(budget, state),
            workspace,
            BuildSummary(budget.Id, state, summary));
    }

    public static bool IsLeaf(Guid categoryId, IReadOnlyCollection<CategoryNodeDto> categories) =>
        !categories.Any(category => category.ParentId == categoryId);

    public static ItemRowModel ToRow(BudgetItemDto item) => new(
        item.Id,
        item.Month,
        BudgetPresentation.MonthName(item.Month),
        item.BudgetFlow,
        BudgetPresentation.FlowTone(item.BudgetFlow),
        MoneyFormat.Format(item.PlannedAmount, item.PlannedCurrency),
        item.RealizedAmount is null ? null : MoneyFormat.Format(item.RealizedAmount.Value, item.RealizedCurrency ?? Currency),
        item.SpentAmount is null ? null : MoneyFormat.Format(item.SpentAmount.Value, item.SpentCurrency ?? Currency));

    private static async Task<ItemsWorkspaceModel> BuildWorkspaceAsync(
        BudgetUi budgets,
        Guid budgetId,
        CategoryNodeDto category,
        bool canEdit,
        CancellationToken cancellationToken)
    {
        Outcome<List<BudgetItemDto>> items = await budgets.ListItemsAsync(budgetId, category.Id, cancellationToken);

        return new ItemsWorkspaceModel(
            budgetId,
            category.Id,
            category.Name,
            items.IsSuccess ? [.. (items.Value ?? []).OrderBy(item => item.Month).Select(ToRow)] : [],
            canEdit,
            items.IsSuccess ? null : items.Error);
    }

    private static CategoryTreeModel BuildTree(
        Guid budgetId,
        BudgetDetailQuery query,
        Outcome<IReadOnlyCollection<CategoryTreeNode>> categories,
        bool canEdit)
    {
        if (!categories.IsSuccess)
        {
            return new CategoryTreeModel(budgetId, query, [], canEdit, categories.Error);
        }

        IReadOnlyList<CategoryNodeModel> nodes = MapNodes(categories.Value ?? [], depth: 0);
        if (query.HasFilter)
        {
            nodes = Filter(nodes, query);
        }

        return new CategoryTreeModel(budgetId, query, nodes, canEdit, Error: null);
    }

    private static List<CategoryNodeModel> MapNodes(IReadOnlyCollection<CategoryTreeNode> nodes, int depth) =>
        [.. nodes.Select(node => new CategoryNodeModel(
            node.Id,
            node.Name,
            depth,
            node.BudgetFlow,
            node.Attribution,
            node.IsDeleted,
            MapNodes(node.Children, depth + 1)))];

    // A category stays when it matches every filter, or when any of its descendants does
    private static List<CategoryNodeModel> Filter(IReadOnlyList<CategoryNodeModel> nodes, BudgetDetailQuery query)
    {
        List<CategoryNodeModel> result = [];

        foreach (CategoryNodeModel node in nodes)
        {
            List<CategoryNodeModel> children = Filter(node.Children, query);

            bool matches =
                (query.Search.Length == 0 || node.Name.Contains(query.Search, StringComparison.OrdinalIgnoreCase))
                && (query.Flow.Length == 0 || node.BudgetFlow == query.Flow)
                && (query.Attribution.Length == 0 || node.Attribution == query.Attribution);

            if (matches || children.Count > 0)
            {
                result.Add(node with { Children = children });
            }
        }

        return result;
    }

    private static LeafCategoriesModel BuildLeaves(BudgetDto budget, BudgetDetailQuery query)
    {
        List<LeafRowModel> rows = [];
        HashSet<Guid> visited = [];

        void Visit(CategoryNodeDto category, int depth)
        {
            if (!visited.Add(category.Id))
            {
                return;
            }

            bool selected = query.Category == category.Id;
            rows.Add(new LeafRowModel(
                category.Id,
                category.Name,
                depth,
                IsLeaf(category.Id, budget.Categories),
                selected,
                (query with { Category = selected ? null : category.Id }).Url(budget.Id)));

            foreach (CategoryNodeDto child in budget.Categories.Where(candidate => candidate.ParentId == category.Id))
            {
                Visit(child, depth + 1);
            }
        }

        foreach (CategoryNodeDto root in budget.Categories.Where(category => category.ParentId is null))
        {
            Visit(root, 0);
        }

        // Categories whose parent is missing still get a row, at the top level
        foreach (CategoryNodeDto orphan in budget.Categories.Where(category => !visited.Contains(category.Id)).ToList())
        {
            Visit(orphan, 0);
        }

        return new LeafCategoriesModel(rows);
    }

    private static BudgetSummaryModel BuildSummary(Guid budgetId, BudgetDetailQuery query, Outcome<BudgetSummaryDto> outcome)
    {
        MnlSegment[] views =
        [
            new(BudgetDetailQuery.Yearly, "Yearly", (query with { View = BudgetDetailQuery.Yearly }).Url(budgetId)),
            new(BudgetDetailQuery.Monthly, "Monthly", (query with { View = BudgetDetailQuery.Monthly }).Url(budgetId))
        ];

        string? previous = query.Month > 1 ? (query with { Month = query.Month - 1 }).Url(budgetId) : null;
        string? next = query.Month < 12 ? (query with { Month = query.Month + 1 }).Url(budgetId) : null;
        string monthLabel = BudgetPresentation.MonthName(query.Month);

        if (outcome is not { IsSuccess: true, Value: { } summary })
        {
            return new BudgetSummaryModel(query.View, views, monthLabel, previous, next, false, false, [], [], null, outcome.Error);
        }

        bool hasRealized = summary.NetRealized is not null;
        bool hasSpent = summary.NetSpent is not null;

        SummaryRowModel Row(CategorySummaryDto category) => new(
            category.Id.ToString(),
            category.Name,
            MoneyFormat.Number(category.PlannedTotal),
            hasRealized ? Amount(category.RealizedTotal) : null,
            hasSpent ? Amount(category.SpentTotal) : null,
            [.. category.Children.Select(Row)]);

        SummaryRowModel net = new(
            "net",
            "Net (Income - Expenses)",
            MoneyFormat.Number(summary.NetPlanned),
            hasRealized ? Amount(summary.NetRealized) : null,
            hasSpent ? Amount(summary.NetSpent) : null,
            []);

        return new BudgetSummaryModel(
            query.View,
            views,
            monthLabel,
            previous,
            next,
            hasRealized,
            hasSpent,
            [.. summary.Income.Select(Row)],
            [.. summary.Expenses.Select(Row)],
            net,
            Error: null);
    }

    // A column that is shown always has a value in every row, so the figures line up
    private static string Amount(decimal? value) => value is null ? "–" : MoneyFormat.Number(value.Value);
}
