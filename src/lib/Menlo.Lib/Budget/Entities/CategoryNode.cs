using Menlo.Lib.Budget.Enums;
using Menlo.Lib.Budget.ValueObjects;
using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Common.Enums;
using Menlo.Lib.Common.ValueObjects;

namespace Menlo.Lib.Budget.Entities;

/// <summary>
/// Represents a node in the budget's category tree.
/// Categories are grouping containers — they do not hold planned amounts.
/// Supports 2-level hierarchy: root → child.
/// </summary>
public sealed class CategoryNode : IEntity<BudgetCategoryId>, IAuditable, ISoftDeletable
{
    internal CategoryNode(
        BudgetCategoryId id,
        CategoryName name,
        BudgetCategoryId? parentId,
        CanonicalCategoryId canonicalCategoryId,
        BudgetFlow budgetFlow,
        Attribution? attribution = null,
        string? description = null,
        string? incomeContributor = null,
        string? responsiblePayer = null)
    {
        Id = id;
        Name = name;
        ParentId = parentId;
        CanonicalCategoryId = canonicalCategoryId;
        BudgetFlow = budgetFlow;
        Attribution = attribution;
        Description = description;
        IncomeContributor = incomeContributor;
        ResponsiblePayer = responsiblePayer;
    }

    // Required by EF Core for materialization
    private CategoryNode()
    {
        Name = null!;
    }

    /// <summary>Gets the unique identifier for this category node.</summary>
    public BudgetCategoryId Id { get; }

    /// <summary>Gets the category name.</summary>
    public CategoryName Name { get; private set; }

    /// <summary>Gets the parent category ID, or null if this is a root category.</summary>
    public BudgetCategoryId? ParentId { get; private set; }

    /// <summary>Gets the canonical category ID for cross-year identity.</summary>
    public CanonicalCategoryId CanonicalCategoryId { get; }

    /// <summary>Gets the budget flow direction for this category.</summary>
    public BudgetFlow BudgetFlow { get; private set; }

    /// <summary>Gets the attribution tag (Main, Rental, ServiceProvider), or null if unset.</summary>
    public Attribution? Attribution { get; private set; }

    /// <summary>Gets an optional description clarifying what belongs in this category.</summary>
    public string? Description { get; private set; }

    /// <summary>Gets the income contributor for this category, if applicable.</summary>
    public string? IncomeContributor { get; private set; }

    /// <summary>Gets the responsible payer for this category, if applicable.</summary>
    public string? ResponsiblePayer { get; private set; }

    /// <summary>
    /// Gets the planned monthly amount. Categories are grouping containers;
    /// amounts live on budget items. Always returns Money.Zero.
    /// </summary>
    public Money PlannedMonthlyAmount => Money.Zero(BudgetCurrency.Zar);

    // IAuditable
    /// <inheritdoc />
    public UserId? CreatedBy { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? CreatedAt { get; private set; }

    /// <inheritdoc />
    public UserId? ModifiedBy { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedAt { get; private set; }

    /// <inheritdoc />
    public void Audit(IAuditStampFactory factory, AuditOperation operation)
    {
        AuditStamp stamp = factory.CreateStamp();
        if (operation == AuditOperation.Create)
        {
            CreatedBy = stamp.ActorId;
            CreatedAt = stamp.Timestamp;
        }

        ModifiedBy = stamp.ActorId;
        ModifiedAt = stamp.Timestamp;
    }

    // ISoftDeletable
    /// <inheritdoc />
    public bool IsDeleted { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? DeletedAt { get; private set; }

    /// <inheritdoc />
    public UserId? DeletedBy { get; private set; }

    /// <inheritdoc />
    public void Delete(ISoftDeleteStampFactory factory)
    {
        SoftDeleteStamp stamp = factory.CreateStamp();
        IsDeleted = true;
        DeletedBy = stamp.ActorId;
        DeletedAt = stamp.Timestamp;
    }

    /// <summary>
    /// Restores this category from soft-deleted state.
    /// </summary>
    internal void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
        DeletedBy = null;
    }

    /// <summary>
    /// Updates the mutable properties of this category.
    /// </summary>
    internal void Update(
        CategoryName name,
        BudgetFlow budgetFlow,
        Attribution? attribution,
        string? description,
        string? incomeContributor,
        string? responsiblePayer)
    {
        Name = name;
        BudgetFlow = budgetFlow;
        Attribution = attribution;
        Description = description;
        IncomeContributor = incomeContributor;
        ResponsiblePayer = responsiblePayer;
    }

    /// <summary>
    /// Changes the parent of this category (re-parent operation).
    /// </summary>
    internal void SetParent(BudgetCategoryId? newParentId)
    {
        ParentId = newParentId;
    }
}
