using CSharpFunctionalExtensions;
using Menlo.Lib.Budget.Errors;
using Menlo.Lib.Budget.ValueObjects;
using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Common.ValueObjects;

namespace Menlo.Lib.Budget.Entities;

/// <summary>
/// Represents a category within a budget for organizing planned amounts.
/// Categories can have subcategories (max depth of 2).
/// </summary>
public sealed class BudgetCategory : IEntity<BudgetCategoryId>
{
    private readonly List<BudgetCategory> _children = [];

    /// <summary>
    /// Private constructor for EF Core hydration.
    /// </summary>
    private BudgetCategory(
        BudgetCategoryId id,
        BudgetId budgetId,
        string name,
        string? description,
        BudgetCategoryId? parentId,
        Money? plannedAmount,
        int displayOrder)
    {
        Id = id;
        BudgetId = budgetId;
        Name = name;
        Description = description;
        ParentId = parentId;
        PlannedAmount = plannedAmount;
        DisplayOrder = displayOrder;
    }

    /// <summary>
    /// Gets the unique identifier for this category.
    /// </summary>
    public BudgetCategoryId Id { get; }

    /// <summary>
    /// Gets the ID of the budget this category belongs to.
    /// </summary>
    public BudgetId BudgetId { get; }

    /// <summary>
    /// Gets the name of the category.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Gets the optional description of the category.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Gets the parent category ID if this is a subcategory.
    /// Null for root categories.
    /// </summary>
    public BudgetCategoryId? ParentId { get; }

    /// <summary>
    /// Gets the planned amount for this category.
    /// Null if no amount has been set.
    /// </summary>
    public Money? PlannedAmount { get; private set; }

    /// <summary>
    /// Gets the display order for sorting categories.
    /// </summary>
    public int DisplayOrder { get; private set; }

    /// <summary>
    /// Gets the children of this category.
    /// </summary>
    public IReadOnlyList<BudgetCategory> Children => _children.AsReadOnly();

    /// <summary>
    /// Gets whether this category is a root category (has no parent).
    /// </summary>
    public bool IsRoot => ParentId is null;

    /// <summary>
    /// Gets whether this category is a leaf category (has no children).
    /// </summary>
    public bool IsLeaf => _children.Count == 0;

    /// <summary>
    /// Creates a new root category.
    /// </summary>
    internal static Result<BudgetCategory, BudgetError> CreateRoot(
        BudgetId budgetId,
        string name,
        string? description,
        int displayOrder)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return new InvalidBudgetDataError("Category name cannot be empty.");
        }

        return new BudgetCategory(
            id: BudgetCategoryId.NewId(),
            budgetId: budgetId,
            name: name.Trim(),
            description: description?.Trim(),
            parentId: null,
            plannedAmount: null,
            displayOrder: displayOrder);
    }

    /// <summary>
    /// Creates a new subcategory under this category.
    /// </summary>
    internal Result<BudgetCategory, BudgetError> CreateChild(
        string name,
        string? description,
        int displayOrder)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return new InvalidBudgetDataError("Category name cannot be empty.");
        }

        // Check for duplicate name among siblings (case-insensitive)
        if (_children.Any(c => c.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            return new DuplicateCategoryNameError(name);
        }

        BudgetCategory child = new(
            id: BudgetCategoryId.NewId(),
            budgetId: BudgetId,
            name: name.Trim(),
            description: description?.Trim(),
            parentId: Id,
            plannedAmount: null,
            displayOrder: displayOrder);

        _children.Add(child);
        return child;
    }

    /// <summary>
    /// Adds an existing child category (used for EF Core hydration and rebuilding the tree).
    /// </summary>
    internal void AddChildInternal(BudgetCategory child)
    {
        _children.Add(child);
    }

    /// <summary>
    /// Renames this category.
    /// </summary>
    internal Result<string, BudgetError> Rename(string newName, IEnumerable<BudgetCategory> siblings)
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            return new InvalidBudgetDataError("Category name cannot be empty.");
        }

        string trimmedName = newName.Trim();

        // Check for duplicate name among siblings (case-insensitive, excluding self)
        if (siblings.Any(c => !c.Id.Equals(Id) && c.Name.Equals(trimmedName, StringComparison.OrdinalIgnoreCase)))
        {
            return new DuplicateCategoryNameError(trimmedName);
        }

        string oldName = Name;
        Name = trimmedName;
        return oldName;
    }

    /// <summary>
    /// Sets the planned amount for this category.
    /// </summary>
    internal Result<Money?, BudgetError> SetPlannedAmount(Money amount, string budgetCurrency)
    {
        if (amount.Amount < 0)
        {
            return new InvalidAmountError("Planned amount cannot be negative.");
        }

        if (!amount.Currency.Equals(budgetCurrency, StringComparison.OrdinalIgnoreCase))
        {
            return new InvalidAmountError($"Amount currency '{amount.Currency}' does not match budget currency '{budgetCurrency}'.");
        }

        Money? previousAmount = PlannedAmount;
        PlannedAmount = amount;
        return previousAmount;
    }

    /// <summary>
    /// Clears the planned amount for this category.
    /// </summary>
    internal Money? ClearPlannedAmount()
    {
        Money? previousAmount = PlannedAmount;
        PlannedAmount = null;
        return previousAmount;
    }

    /// <summary>
    /// Updates the display order of this category.
    /// </summary>
    internal void SetDisplayOrder(int newOrder)
    {
        DisplayOrder = newOrder;
    }

    /// <summary>
    /// Updates the description of this category.
    /// </summary>
    internal void SetDescription(string? description)
    {
        Description = description?.Trim();
    }

    /// <summary>
    /// Removes a child category from this category's children.
    /// </summary>
    internal bool RemoveChild(BudgetCategoryId childId)
    {
        BudgetCategory? child = _children.FirstOrDefault(c => c.Id.Equals(childId));
        if (child is null)
        {
            return false;
        }

        return _children.Remove(child);
    }

    /// <summary>
    /// Calculates the total amount for this category including all descendants.
    /// </summary>
    public Money CalculateTotal(string currency)
    {
        Money total = PlannedAmount ?? Money.Zero(currency);

        foreach (BudgetCategory child in _children)
        {
            Money childTotal = child.CalculateTotal(currency);
            Result<Money, Common.Abstractions.Error> addResult = total.Add(childTotal);
            if (addResult.IsSuccess)
            {
                total = addResult.Value;
            }
        }

        return total;
    }
}
