namespace Menlo.Lib.Budget.Models;

using Menlo.Lib.Budget.ValueObjects;
using Menlo.Lib.Common;

/// <summary>
/// Response model for budget details.
/// </summary>
public sealed record BudgetResponse(
    Guid Id,
    string Name,
    int Year,
    int Month,
    string Currency,
    string Status,
    IReadOnlyList<BudgetCategoryResponse> Categories,
    MoneyResponse Total,
    DateTimeOffset? CreatedAt,
    DateTimeOffset? ModifiedAt);

/// <summary>
/// Response model for budget category details.
/// </summary>
public sealed record BudgetCategoryResponse(
    Guid Id,
    string Name,
    string? Description,
    Guid? ParentId,
    MoneyResponse? PlannedAmount,
    int DisplayOrder,
    bool IsRoot,
    bool IsLeaf,
    IReadOnlyList<BudgetCategoryResponse> Children);

/// <summary>
/// Response model for Money value object.
/// </summary>
public sealed record MoneyResponse(
    decimal Amount,
    string Currency);

/// <summary>
/// Summary response model for budget list.
/// </summary>
public sealed record BudgetSummaryResponse(
    Guid Id,
    string Name,
    int Year,
    int Month,
    string Currency,
    string Status,
    MoneyResponse Total,
    int CategoryCount,
    DateTimeOffset? CreatedAt);
