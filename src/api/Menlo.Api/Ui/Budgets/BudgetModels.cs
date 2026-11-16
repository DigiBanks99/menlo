using Menlo.Api.Ui.Shared;
using Menlo.Ui.Components;

namespace Menlo.Api.Ui.Budgets;

// View models for the budget pages. Each is everything its component needs, already formatted:
// components render them and decide nothing.

/// <summary>The budget list page.</summary>
public sealed record BudgetListPageModel(
    IReadOnlyList<BudgetCardModel> Budgets,
    int? NextBudgetYear,
    bool CanEdit,
    string? LoadError,
    string? CreateError);

/// <summary>One budget in the list.</summary>
public sealed record BudgetCardModel(
    Guid Id,
    int Year,
    string Status,
    MnlTone StatusTone,
    string CategoryCount,
    decimal SpentPercent,
    MnlTone SpendTone,
    string SpendText,
    string PercentText);

/// <summary>The budget detail page.</summary>
public sealed record BudgetDetailPageModel(
    Guid Id,
    int Year,
    string Status,
    MnlTone StatusTone,
    string TotalPlanned,
    int? NextYearToCreate,
    bool CanEdit,
    CategoryTreeModel Tree,
    LeafCategoriesModel Leaves,
    ItemsWorkspaceModel? Workspace,
    BudgetSummaryModel Summary);

/// <summary>The category tree and its filters.</summary>
public sealed record CategoryTreeModel(
    Guid BudgetId,
    BudgetDetailQuery Query,
    IReadOnlyList<CategoryNodeModel> Nodes,
    bool CanEdit,
    string? Error);

/// <summary>A category in the tree.</summary>
public sealed record CategoryNodeModel(
    Guid Id,
    string Name,
    int Depth,
    string BudgetFlow,
    string? Attribution,
    bool IsDeleted,
    IReadOnlyList<CategoryNodeModel> Children)
{
    public bool HasChildren => Children.Count > 0;
}

/// <summary>The flat list of categories from which a leaf's items are opened.</summary>
public sealed record LeafCategoriesModel(IReadOnlyList<LeafRowModel> Rows);

/// <summary>A category in the leaf list.</summary>
/// <param name="ToggleUrl">The page address that opens this category's items, or closes them when it is selected.</param>
public sealed record LeafRowModel(Guid Id, string Name, int Depth, bool IsLeaf, bool IsSelected, string ToggleUrl)
{
    /// <summary>Four levels down is deep enough to suggest flattening.</summary>
    public bool IsDeep => Depth >= 4;
}

/// <summary>The items of one leaf category.</summary>
public sealed record ItemsWorkspaceModel(
    Guid BudgetId,
    Guid CategoryId,
    string CategoryName,
    IReadOnlyList<ItemRowModel> Items,
    bool CanEdit,
    string? Error)
{
    public string ItemsUrl => $"/budgets/{BudgetId}/categories/{CategoryId}/items";
}

/// <summary>One budget item.</summary>
public sealed record ItemRowModel(
    Guid Id,
    int Month,
    string MonthName,
    string BudgetFlow,
    MnlTone FlowTone,
    string Planned,
    string? Realized,
    string? Spent);

/// <summary>The income, expenses and net of a budget for a month or the year.</summary>
public sealed record BudgetSummaryModel(
    string View,
    IReadOnlyList<MnlSegment> Views,
    string MonthLabel,
    string? PreviousMonthUrl,
    string? NextMonthUrl,
    bool HasRealized,
    bool HasSpent,
    IReadOnlyList<SummaryRowModel> Income,
    IReadOnlyList<SummaryRowModel> Expenses,
    SummaryRowModel? Net,
    string? Error)
{
    public bool IsMonthly => View == BudgetDetailQuery.Monthly;
}

/// <summary>A line of the summary: a category, or the net.</summary>
public sealed record SummaryRowModel(
    string Key,
    string Name,
    string Planned,
    string? Realized,
    string? Spent,
    IReadOnlyList<SummaryRowModel> Children);

/// <summary>The form that creates or edits a category.</summary>
public sealed record CategoryFormModel(
    Guid BudgetId,
    Guid? CategoryId,
    Guid? ParentId,
    string Name,
    string Description,
    string BudgetFlow,
    string Attribution,
    string IncomeContributor,
    string ResponsiblePayer,
    FormErrors Errors)
{
    public bool IsEdit => CategoryId is not null;

    public string Title => IsEdit ? "Edit category" : ParentId is not null ? "Add child category" : "Add category";

    public string Subtitle => IsEdit
        ? "Update the selected category details."
        : ParentId is not null
            ? "Create a child category within the selected branch."
            : "Add a new top-level category to this budget.";

    public string Action => IsEdit
        ? $"/budgets/{BudgetId}/categories/{CategoryId}"
        : $"/budgets/{BudgetId}/categories";
}

/// <summary>One payer's share of an item.</summary>
public sealed record PayerRowModel(string UserId, string Percent);

/// <summary>One attribution's share of an item.</summary>
public sealed record AttributionRowModel(string Attribution, string Percent);

/// <summary>The form that creates or edits a budget item, or creates one for every month.</summary>
/// <param name="Mode">Which of the three forms this is.</param>
public sealed record ItemFormModel(
    ItemFormMode Mode,
    Guid BudgetId,
    Guid CategoryId,
    Guid? ItemId,
    string Month,
    string BudgetFlow,
    decimal? PlannedAmount,
    decimal? RealizedAmount,
    decimal? SpentAmount,
    IReadOnlyList<PayerRowModel> Payers,
    IReadOnlyList<AttributionRowModel> Attributions,
    IReadOnlyList<MnlOption> Members,
    FormErrors Errors)
{
    private string ItemsUrl => $"/budgets/{BudgetId}/categories/{CategoryId}/items";

    public string Action => Mode switch
    {
        ItemFormMode.Edit => $"{ItemsUrl}/{ItemId}",
        ItemFormMode.Bulk => $"{ItemsUrl}/bulk",
        _ => ItemsUrl
    };

    public string Title => Mode switch
    {
        ItemFormMode.Edit => $"Edit {BudgetPresentation.MonthName(int.TryParse(Month, out int month) ? month : 0)}",
        ItemFormMode.Bulk => "Add line items for all months",
        _ => "Add a budget item"
    };

    public string Subtitle => Mode switch
    {
        ItemFormMode.Edit => "Update the amounts and splits of this line item.",
        ItemFormMode.Bulk => "Create the same item across every month of the year in one go.",
        _ => "Capture a single monthly item for this category."
    };

    public string SubmitLabel => Mode == ItemFormMode.Edit ? "Update" : "Create";

    public string PayerTotal => SplitTotal(Payers.Select(payer => payer.Percent));

    public string AttributionTotal => SplitTotal(Attributions.Select(attribution => attribution.Percent));

    public bool PayerOnTarget => PayerTotal == "100";

    public bool AttributionOnTarget => AttributionTotal == "100";

    private static string SplitTotal(IEnumerable<string> percents) =>
        percents.Sum(percent => int.TryParse(percent, out int value) ? value : 0).ToString(System.Globalization.CultureInfo.InvariantCulture);
}

/// <summary>Which item form is shown.</summary>
public enum ItemFormMode
{
    /// <summary>One item for one month.</summary>
    Create,

    /// <summary>An existing item.</summary>
    Edit,

    /// <summary>One item for each of the twelve months.</summary>
    Bulk
}

/// <summary>A small form that takes one amount: record a bill, record a payment, or fill forward.</summary>
public sealed record AmountFormModel(
    string Title,
    string Subtitle,
    string Label,
    string SubmitLabel,
    string Action,
    decimal? Amount,
    FormErrors Errors)
{
    /// <summary>The HTTP method the form submits with: "put" or "post".</summary>
    public string Method { get; init; } = "put";
}

/// <summary>The "are you sure" panel for deleting an item.</summary>
public sealed record DeleteItemModel(string Action, string? Error);
