using CSharpFunctionalExtensions;
using Menlo.Lib.Budget.Enums;
using Menlo.Lib.Budget.Errors;
using Menlo.Lib.Budget.Events;
using Menlo.Lib.Budget.ValueObjects;
using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Common.Enums;
using Menlo.Lib.Common.ValueObjects;

namespace Menlo.Lib.Budget.Entities;

/// <summary>
/// Aggregate root representing a monthly budget with hierarchical categories.
/// </summary>
public sealed class Budget : IAggregateRoot<BudgetId>, IHasDomainEvents, IAuditable
{
    /// <summary>
    /// Maximum depth for category hierarchy (root = 0, subcategory = 1).
    /// </summary>
    public const int MaxCategoryDepth = 2;

    private readonly List<IDomainEvent> _domainEvents = [];
    private readonly List<BudgetCategory> _categories = [];

    /// <summary>
    /// Parameterless constructor for EF Core (required for ComplexProperty mapping).
    /// </summary>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor
    private Budget()
    {
    }
#pragma warning restore CS8618

    /// <summary>
    /// Private constructor for EF Core hydration.
    /// </summary>
    private Budget(
        BudgetId id,
        UserId ownerId,
        string name,
        BudgetPeriod period,
        string currency,
        BudgetStatus status,
        UserId? createdBy,
        DateTimeOffset? createdAt,
        UserId? modifiedBy,
        DateTimeOffset? modifiedAt)
    {
        Id = id;
        OwnerId = ownerId;
        Name = name;
        Period = period;
        Currency = currency;
        Status = status;
        CreatedBy = createdBy;
        CreatedAt = createdAt;
        ModifiedBy = modifiedBy;
        ModifiedAt = modifiedAt;
    }

    /// <summary>
    /// Gets the unique identifier for this budget.
    /// </summary>
    public BudgetId Id { get; }

    /// <summary>
    /// Gets the ID of the user who owns this budget.
    /// </summary>
    public UserId OwnerId { get; }

    /// <summary>
    /// Gets the name of the budget.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Gets the budget period (year and month).
    /// </summary>
    public BudgetPeriod Period { get; }

    /// <summary>
    /// Gets the currency code for all amounts in this budget (ISO 4217).
    /// </summary>
    public string Currency { get; }

    /// <summary>
    /// Gets the current status of the budget.
    /// </summary>
    public BudgetStatus Status { get; private set; }

    /// <summary>
    /// Gets the root categories of this budget.
    /// </summary>
    public IReadOnlyList<BudgetCategory> Categories => _categories.AsReadOnly();

    // IAuditable implementation
    /// <inheritdoc />
    public UserId? CreatedBy { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? CreatedAt { get; private set; }

    /// <inheritdoc />
    public UserId? ModifiedBy { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedAt { get; private set; }

    // IHasDomainEvents implementation
    /// <inheritdoc />
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <inheritdoc />
    public void AddDomainEvent<TEvent>(TEvent domainEvent) where TEvent : IDomainEvent
    {
        _domainEvents.Add(domainEvent);
    }

    /// <inheritdoc />
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

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

    /// <summary>
    /// Factory method to create a new Budget.
    /// </summary>
    /// <param name="ownerId">The ID of the user creating the budget.</param>
    /// <param name="name">The name of the budget.</param>
    /// <param name="period">The budget period (year and month).</param>
    /// <param name="currency">The currency code (ISO 4217).</param>
    /// <returns>A Result containing the new Budget or an error.</returns>
    public static Result<Budget, BudgetError> Create(
        UserId ownerId,
        string name,
        BudgetPeriod period,
        string currency)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return new InvalidBudgetDataError("Budget name cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            return new InvalidBudgetDataError("Currency cannot be empty.");
        }

        string trimmedCurrency = currency.Trim().ToUpperInvariant();
        if (trimmedCurrency.Length != 3)
        {
            return new InvalidBudgetDataError("Currency must be a valid 3-letter ISO 4217 code.");
        }

        Budget budget = new(
            id: BudgetId.NewId(),
            ownerId: ownerId,
            name: name.Trim(),
            period: period,
            currency: trimmedCurrency,
            status: BudgetStatus.Draft,
            createdBy: null,
            createdAt: null,
            modifiedBy: null,
            modifiedAt: null);

        budget.AddDomainEvent(new BudgetCreatedEvent(
            budget.Id,
            budget.Name,
            budget.Period,
            budget.Currency,
            DateTimeOffset.UtcNow));

        return budget;
    }

    /// <summary>
    /// Adds a root category to the budget.
    /// </summary>
    /// <param name="name">The name of the category.</param>
    /// <param name="description">Optional description.</param>
    /// <returns>Result containing the new category or an error.</returns>
    public Result<BudgetCategory, BudgetError> AddCategory(string name, string? description = null)
    {
        // Check for duplicate name among root categories (case-insensitive)
        if (_categories.Any(c => c.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            return new DuplicateCategoryNameError(name);
        }

        int displayOrder = _categories.Count;
        Result<BudgetCategory, BudgetError> result = BudgetCategory.CreateRoot(Id, name, description, displayOrder);

        if (result.IsFailure)
        {
            return result;
        }

        BudgetCategory category = result.Value;
        _categories.Add(category);

        AddDomainEvent(new CategoryAddedEvent(
            Id,
            category.Id,
            category.Name,
            null,
            DateTimeOffset.UtcNow));

        return category;
    }

    /// <summary>
    /// Adds a subcategory under an existing category.
    /// </summary>
    /// <param name="parentId">The ID of the parent category.</param>
    /// <param name="name">The name of the subcategory.</param>
    /// <param name="description">Optional description.</param>
    /// <returns>Result containing the new subcategory or an error.</returns>
    public Result<BudgetCategory, BudgetError> AddSubcategory(
        BudgetCategoryId parentId,
        string name,
        string? description = null)
    {
        BudgetCategory? parent = FindCategory(parentId);
        if (parent is null)
        {
            return new CategoryNotFoundError(parentId.Value);
        }

        // Check max depth: only root categories can have children
        if (!parent.IsRoot)
        {
            return new MaxDepthExceededError();
        }

        int displayOrder = parent.Children.Count;
        Result<BudgetCategory, BudgetError> result = parent.CreateChild(name, description, displayOrder);

        if (result.IsFailure)
        {
            return result;
        }

        BudgetCategory subcategory = result.Value;

        AddDomainEvent(new CategoryAddedEvent(
            Id,
            subcategory.Id,
            subcategory.Name,
            parentId,
            DateTimeOffset.UtcNow));

        return subcategory;
    }

    /// <summary>
    /// Renames a category.
    /// </summary>
    /// <param name="categoryId">The ID of the category to rename.</param>
    /// <param name="newName">The new name for the category.</param>
    /// <returns>Result indicating success or failure.</returns>
    public Result<bool, BudgetError> RenameCategory(BudgetCategoryId categoryId, string newName)
    {
        (BudgetCategory? category, BudgetCategory? parent) = FindCategoryWithParent(categoryId);

        if (category is null)
        {
            return new CategoryNotFoundError(categoryId.Value);
        }

        // Get siblings for uniqueness check
        IEnumerable<BudgetCategory> siblings = parent is null
            ? _categories
            : parent.Children;

        Result<string, BudgetError> result = category.Rename(newName, siblings);

        if (result.IsFailure)
        {
            return result.Error;
        }

        AddDomainEvent(new CategoryRenamedEvent(
            Id,
            categoryId,
            result.Value,
            category.Name,
            DateTimeOffset.UtcNow));

        return true;
    }

    /// <summary>
    /// Updates the description of a category.
    /// </summary>
    /// <param name="categoryId">The ID of the category to update.</param>
    /// <param name="description">The new description (can be null).</param>
    /// <returns>Result indicating success or failure.</returns>
    public Result<bool, BudgetError> UpdateCategoryDescription(BudgetCategoryId categoryId, string? description)
    {
        BudgetCategory? category = FindCategory(categoryId);

        if (category is null)
        {
            return new CategoryNotFoundError(categoryId.Value);
        }

        category.UpdateDescription(description);

        return true;
    }

    /// <summary>
    /// Removes a category from the budget.
    /// </summary>
    /// <param name="categoryId">The ID of the category to remove.</param>
    /// <returns>Result indicating success or failure.</returns>
    public Result<bool, BudgetError> RemoveCategory(BudgetCategoryId categoryId)
    {
        (BudgetCategory? category, BudgetCategory? parent) = FindCategoryWithParent(categoryId);

        if (category is null)
        {
            return new CategoryNotFoundError(categoryId.Value);
        }

        // Cannot remove if has children
        if (!category.IsLeaf)
        {
            return new CategoryHasChildrenError(categoryId.Value);
        }

        // Cannot remove if has planned amount
        if (category.PlannedAmount.HasValue)
        {
            return new CategoryHasPlannedAmountError(categoryId.Value);
        }

        string name = category.Name;

        if (parent is null)
        {
            _categories.Remove(category);
        }
        else
        {
            parent.RemoveChild(categoryId);
        }

        AddDomainEvent(new CategoryRemovedEvent(
            Id,
            categoryId,
            name,
            DateTimeOffset.UtcNow));

        return true;
    }

    /// <summary>
    /// Sets the planned amount for a category.
    /// </summary>
    /// <param name="categoryId">The ID of the category.</param>
    /// <param name="amount">The planned amount to set.</param>
    /// <returns>Result indicating success or failure.</returns>
    public Result<bool, BudgetError> SetPlannedAmount(BudgetCategoryId categoryId, Money amount)
    {
        BudgetCategory? category = FindCategory(categoryId);
        if (category is null)
        {
            return new CategoryNotFoundError(categoryId.Value);
        }

        Result<Money?, BudgetError> result = category.SetPlannedAmount(amount, Currency);

        if (result.IsFailure)
        {
            return result.Error;
        }

        AddDomainEvent(new PlannedAmountSetEvent(
            Id,
            categoryId,
            amount,
            DateTimeOffset.UtcNow));

        return true;
    }

    /// <summary>
    /// Clears the planned amount for a category.
    /// </summary>
    /// <param name="categoryId">The ID of the category.</param>
    /// <returns>Result indicating success or failure.</returns>
    public Result<bool, BudgetError> ClearPlannedAmount(BudgetCategoryId categoryId)
    {
        BudgetCategory? category = FindCategory(categoryId);
        if (category is null)
        {
            return new CategoryNotFoundError(categoryId.Value);
        }

        Money? previousAmount = category.ClearPlannedAmount();

        AddDomainEvent(new PlannedAmountClearedEvent(
            Id,
            categoryId,
            previousAmount,
            DateTimeOffset.UtcNow));

        return true;
    }

    /// <summary>
    /// Updates the display order of a category.
    /// </summary>
    /// <param name="categoryId">The ID of the category.</param>
    /// <param name="newOrder">The new display order.</param>
    /// <returns>Result indicating success or failure.</returns>
    public Result<bool, BudgetError> ReorderCategory(BudgetCategoryId categoryId, int newOrder)
    {
        BudgetCategory? category = FindCategory(categoryId);
        if (category is null)
        {
            return new CategoryNotFoundError(categoryId.Value);
        }

        category.SetDisplayOrder(newOrder);
        return true;
    }

    /// <summary>
    /// Activates the budget, transitioning from Draft to Active status.
    /// </summary>
    /// <returns>Result indicating success or failure.</returns>
    public Result<bool, BudgetError> Activate()
    {
        if (Status != BudgetStatus.Draft)
        {
            return new InvalidStatusTransitionError("activate", Status.ToString());
        }

        // Validate: must have at least one category with a non-zero planned amount
        bool hasNonZeroPlanned = GetAllCategories()
            .Any(c => c.PlannedAmount.HasValue && c.PlannedAmount.Value.Amount > 0);

        if (!hasNonZeroPlanned)
        {
            return new ActivationValidationError("Budget must have at least one category with a non-zero planned amount.");
        }

        Status = BudgetStatus.Active;

        AddDomainEvent(new BudgetActivatedEvent(Id, DateTimeOffset.UtcNow));

        return true;
    }

    /// <summary>
    /// Updates the budget name.
    /// </summary>
    /// <param name="newName">The new name for the budget.</param>
    /// <returns>Result indicating success or failure.</returns>
    public Result<bool, BudgetError> UpdateName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            return new InvalidBudgetDataError("Budget name cannot be empty.");
        }

        Name = newName.Trim();
        return true;
    }

    /// <summary>
    /// Gets the total planned amount for the entire budget.
    /// </summary>
    /// <returns>The total of all category planned amounts.</returns>
    public Money GetTotal()
    {
        Money total = Money.Zero(Currency);

        foreach (BudgetCategory category in _categories)
        {
            Money categoryTotal = category.CalculateTotal(Currency);
            Result<Money, Error> addResult = total.Add(categoryTotal);
            if (addResult.IsSuccess)
            {
                total = addResult.Value;
            }
        }

        return total;
    }

    /// <summary>
    /// Gets a snapshot of all category totals.
    /// </summary>
    /// <returns>Dictionary mapping category IDs to their totals (including descendants).</returns>
    public Dictionary<BudgetCategoryId, Money> GetCategoryTotals()
    {
        Dictionary<BudgetCategoryId, Money> totals = [];

        foreach (BudgetCategory category in GetAllCategories())
        {
            totals[category.Id] = category.CalculateTotal(Currency);
        }

        return totals;
    }

    /// <summary>
    /// Finds a category by ID anywhere in the hierarchy.
    /// </summary>
    /// <param name="categoryId">The ID of the category to find.</param>
    /// <returns>The category if found; null otherwise.</returns>
    public BudgetCategory? FindCategory(BudgetCategoryId categoryId)
    {
        foreach (BudgetCategory root in _categories)
        {
            if (root.Id.Equals(categoryId))
            {
                return root;
            }

            BudgetCategory? child = root.Children.FirstOrDefault(c => c.Id.Equals(categoryId));
            if (child is not null)
            {
                return child;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets all categories in a flat list (root and children).
    /// </summary>
    /// <returns>All categories in the budget.</returns>
    public IEnumerable<BudgetCategory> GetAllCategories()
    {
        foreach (BudgetCategory root in _categories)
        {
            yield return root;
            foreach (BudgetCategory child in root.Children)
            {
                yield return child;
            }
        }
    }

    /// <summary>
    /// Adds a category to the internal collection (for EF Core hydration).
    /// </summary>
    internal void AddCategoryInternal(BudgetCategory category)
    {
        _categories.Add(category);
    }

    /// <summary>
    /// Finds a category and its parent.
    /// </summary>
    private (BudgetCategory? Category, BudgetCategory? Parent) FindCategoryWithParent(BudgetCategoryId categoryId)
    {
        foreach (BudgetCategory root in _categories)
        {
            if (root.Id.Equals(categoryId))
            {
                return (root, null);
            }

            BudgetCategory? child = root.Children.FirstOrDefault(c => c.Id.Equals(categoryId));
            if (child is not null)
            {
                return (child, root);
            }
        }

        return (null, null);
    }
}
