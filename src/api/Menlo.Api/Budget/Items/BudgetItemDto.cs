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

/// <summary>
/// Request to update an existing budget item. All fields are optional; only provided fields are changed.
/// </summary>
public sealed record UpdateBudgetItemRequest(
    decimal? PlannedAmount = null,
    string? PlannedCurrency = null,
    decimal? RealizedAmount = null,
    string? RealizedCurrency = null,
    decimal? SpentAmount = null,
    string? SpentCurrency = null,
    IReadOnlyList<PayerAllocationDto>? PayerSplit = null,
    IReadOnlyList<AttributionAllocationDto>? AttributionSplit = null);

/// <summary>
/// Request to realize a budget item (record actual bill/payslip amount).
/// </summary>
public sealed record RealizeBudgetItemRequest(decimal Amount, string Currency = "ZAR");

/// <summary>
/// Request to record a budget item as spent (payment made).
/// </summary>
public sealed record RecordBudgetItemSpentRequest(decimal Amount, string Currency = "ZAR");

/// <summary>
/// Request to bulk-create budget items for all 12 months of a category.
/// </summary>
public sealed record BulkCreateBudgetItemRequest(
    string BudgetFlow,
    decimal Amount,
    string Currency,
    IReadOnlyList<PayerAllocationDto> PayerSplit,
    IReadOnlyList<AttributionAllocationDto> AttributionSplit);
