using Menlo.Lib.Budget.ValueObjects;
using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Common.ValueObjects;

namespace Menlo.Lib.Budget.Entities;

/// <summary>
/// Represents a node in the budget's category tree.
/// Each node has an optional parent reference enabling unlimited nesting depth.
/// </summary>
public sealed class CategoryNode : IEntity<BudgetCategoryId>
{
    internal CategoryNode(
        BudgetCategoryId id,
        CategoryName name,
        BudgetCategoryId? parentId,
        Money plannedMonthlyAmount)
    {
        Id = id;
        Name = name;
        ParentId = parentId;
        PlannedMonthlyAmount = plannedMonthlyAmount;
    }

    /// <summary>
    /// Gets the unique identifier for this category node.
    /// </summary>
    public BudgetCategoryId Id { get; }

    /// <summary>
    /// Gets the category name.
    /// </summary>
    public CategoryName Name { get; }

    /// <summary>
    /// Gets the parent category ID, or null if this is a root category.
    /// </summary>
    public BudgetCategoryId? ParentId { get; }

    /// <summary>
    /// Gets the default planned monthly amount for this category.
    /// Defaults to zero ZAR.
    /// </summary>
    public Money PlannedMonthlyAmount { get; private set; }

    /// <summary>
    /// Sets the planned monthly amount for this category.
    /// </summary>
    /// <param name="amount">The new planned monthly amount.</param>
    internal void SetPlannedAmount(Money amount)
    {
        PlannedMonthlyAmount = amount;
    }
}
