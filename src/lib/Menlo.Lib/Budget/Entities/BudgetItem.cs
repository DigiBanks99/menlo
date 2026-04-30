using Menlo.Lib.Budget.Enums;
using Menlo.Lib.Budget.ValueObjects;
using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Common.Enums;
using Menlo.Lib.Common.ValueObjects;

namespace Menlo.Lib.Budget.Entities;

/// <summary>
/// Represents a single budget line item for one month within a leaf category.
/// Carries planned, realized, and spent amounts along with payer and attribution splits.
/// </summary>
public sealed class BudgetItem : IEntity<BudgetItemId>, IAuditable, ISoftDeletable
{
    internal BudgetItem(
        BudgetItemId id,
        BudgetId budgetId,
        BudgetCategoryId categoryId,
        int month,
        BudgetFlow budgetFlow,
        Money plannedAmount,
        PayerSplit payerSplit,
        AttributionSplit attributionSplit,
        Guid? adjustmentRuleId = null,
        bool isManualOverride = true)
    {
        Id = id;
        BudgetId = budgetId;
        CategoryId = categoryId;
        Month = month;
        BudgetFlow = budgetFlow;
        PlannedAmount = plannedAmount;
        PayerSplit = payerSplit;
        AttributionSplit = attributionSplit;
        AdjustmentRuleId = adjustmentRuleId;
        IsManualOverride = isManualOverride;
    }

    // Required by EF Core for materialization
    private BudgetItem()
    {
        PayerSplit = null!;
        AttributionSplit = null!;
    }

    /// <summary>Gets the unique identifier for this budget item.</summary>
    public BudgetItemId Id { get; }

    /// <summary>Gets the budget this item belongs to.</summary>
    public BudgetId BudgetId { get; }

    /// <summary>Gets the leaf category this item belongs to.</summary>
    public BudgetCategoryId CategoryId { get; }

    /// <summary>Gets the month (1-12) this item covers.</summary>
    public int Month { get; }

    /// <summary>Gets whether this item is income or expense.</summary>
    public BudgetFlow BudgetFlow { get; }

    /// <summary>Gets the planned amount (set at creation).</summary>
    public Money PlannedAmount { get; private set; }

    /// <summary>Gets the realized amount (set when bill arrives). Null until realized.</summary>
    public Money? RealizedAmount { get; private set; }

    /// <summary>Gets the spent amount (set when payment clears). Null until spent.</summary>
    public Money? SpentAmount { get; private set; }

    /// <summary>Gets the payer responsibility split.</summary>
    public PayerSplit PayerSplit { get; private set; }

    /// <summary>Gets the attribution purpose split.</summary>
    public AttributionSplit AttributionSplit { get; private set; }

    /// <summary>Gets the optional adjustment rule ID for future rules engine.</summary>
    public Guid? AdjustmentRuleId { get; private set; }

    /// <summary>Gets whether this item was manually set (true) or derived from a rule (future).</summary>
    public bool IsManualOverride { get; private set; }

    // IAuditable
    public UserId? CreatedBy { get; private set; }
    public DateTimeOffset? CreatedAt { get; private set; }
    public UserId? ModifiedBy { get; private set; }
    public DateTimeOffset? ModifiedAt { get; private set; }

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
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    public UserId? DeletedBy { get; private set; }

    public void Delete(ISoftDeleteStampFactory factory)
    {
        SoftDeleteStamp stamp = factory.CreateStamp();
        IsDeleted = true;
        DeletedBy = stamp.ActorId;
        DeletedAt = stamp.Timestamp;
    }
}
