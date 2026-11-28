using Menlo.Lib.Budget.ValueObjects;
using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Common.Enums;
using Menlo.Lib.Common.ValueObjects;

namespace Menlo.Lib.Budget.Entities;

/// <summary>
/// What a budget's lines are for, such as home, school or transport. A category is part of its budget and is only
/// changed through it. Categories nest at most two deep: a top-level category may have subcategories, and a
/// subcategory can't have any of its own.
/// </summary>
public sealed class Category : IEntity<CategoryId>, IAuditable
{
    /// <summary>The longest name a category can have.</summary>
    public const int MaxNameLength = 100;

    /// <summary>
    /// Private constructor for EF Core hydration.
    /// </summary>
    private Category(
        CategoryId id,
        BudgetId budgetId,
        string name,
        CategoryId? parentId,
        UserId? createdBy,
        DateTimeOffset? createdAt,
        UserId? modifiedBy,
        DateTimeOffset? modifiedAt)
    {
        Id = id;
        BudgetId = budgetId;
        Name = name;
        ParentId = parentId;
        CreatedBy = createdBy;
        CreatedAt = createdAt;
        ModifiedBy = modifiedBy;
        ModifiedAt = modifiedAt;
    }

    /// <summary>
    /// Gets the unique identifier for this category.
    /// </summary>
    public CategoryId Id { get; }

    /// <summary>
    /// Gets the ID of the budget this category belongs to.
    /// </summary>
    public BudgetId BudgetId { get; }

    /// <summary>
    /// Gets the category's name, a label such as "Home" or "School fees". It is unique among its siblings, ignoring
    /// case.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Gets the ID of the top-level category this one sits under, or <see langword="null"/> when it is top-level.
    /// </summary>
    public CategoryId? ParentId { get; private set; }

    /// <summary>
    /// Gets whether this is a top-level category, one without a parent.
    /// </summary>
    public bool IsTopLevel => ParentId is null;

    /// <inheritdoc />
    public UserId? CreatedBy { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? CreatedAt { get; private set; }

    /// <inheritdoc />
    public UserId? ModifiedBy { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedAt { get; private set; }

    /// <summary>
    /// Makes a new category for a budget. Only the budget calls this, once it has checked its rules.
    /// </summary>
    internal static Category Create(BudgetId budgetId, string name, CategoryId? parentId) => new(
        id: CategoryId.NewId(),
        budgetId: budgetId,
        name: name,
        parentId: parentId,
        createdBy: null,
        createdAt: null,
        modifiedBy: null,
        modifiedAt: null);

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
}
