namespace Menlo.Api.Budget.Summary;

public sealed record CategorySummaryDto(
    Guid Id,
    string Name,
    decimal PlannedTotal,
    decimal? RealizedTotal,
    decimal? SpentTotal,
    IReadOnlyList<CategorySummaryDto> Children);

public sealed record BudgetSummaryDto(
    Guid BudgetId,
    int Year,
    int Month,
    IReadOnlyList<CategorySummaryDto> Income,
    IReadOnlyList<CategorySummaryDto> Expenses,
    decimal NetPlanned,
    decimal? NetRealized,
    decimal? NetSpent);
