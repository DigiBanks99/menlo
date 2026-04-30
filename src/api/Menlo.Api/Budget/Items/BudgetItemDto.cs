namespace Menlo.Api.Budget.Items;

public sealed record PayerAllocationDto(Guid UserId, int Percent);

public sealed record AttributionAllocationDto(string Attribution, int Percent);

public sealed record BudgetItemDto(
    Guid Id,
    Guid BudgetId,
    Guid CategoryId,
    int Month,
    string BudgetFlow,
    decimal PlannedAmount,
    string PlannedCurrency,
    decimal? RealizedAmount,
    string? RealizedCurrency,
    decimal? SpentAmount,
    string? SpentCurrency,
    IReadOnlyList<PayerAllocationDto> PayerSplit,
    IReadOnlyList<AttributionAllocationDto> AttributionSplit,
    Guid? AdjustmentRuleId,
    bool IsManualOverride);

public sealed record CreateBudgetItemRequest(
    int Month,
    string BudgetFlow,
    decimal PlannedAmount,
    string PlannedCurrency,
    IReadOnlyList<PayerAllocationDto> PayerSplit,
    IReadOnlyList<AttributionAllocationDto> AttributionSplit,
    Guid? AdjustmentRuleId = null,
    bool IsManualOverride = true);
