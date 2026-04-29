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
/// The default currency used throughout the Budget bounded context.
/// </summary>
internal static class BudgetCurrency
{
    internal const string Zar = "ZAR";
}

/// <summary>
/// Aggregate root representing a household's budget for a calendar year.
/// One budget per household per year; uniqueness is enforced at domain and database level.
/// </summary>
public sealed class Budget : IAggregateRoot<BudgetId>, IHasDomainEvents, IAuditable, ISoftDeletable
{
    private readonly List<IDomainEvent> _domainEvents = [];
    private readonly List<CategoryNode> _categories = [];

    private Budget(
        BudgetId id,
        HouseholdId householdId,
        int year,
        BudgetStatus status,
        UserId? createdBy,
        DateTimeOffset? createdAt,
        UserId? modifiedBy,
        DateTimeOffset? modifiedAt,
        bool isDeleted,
        DateTimeOffset? deletedAt,
        UserId? deletedBy)
    {
        Id = id;
        HouseholdId = householdId;
        Year = year;
        Status = status;
        CreatedBy = createdBy;
        CreatedAt = createdAt;
        ModifiedBy = modifiedBy;
        ModifiedAt = modifiedAt;
        IsDeleted = isDeleted;
        DeletedAt = deletedAt;
        DeletedBy = deletedBy;
    }

    /// <summary>
    /// Gets the unique identifier for this budget.
    /// </summary>
    public BudgetId Id { get; }

    /// <summary>
    /// Gets the household this budget belongs to.
    /// </summary>
    public HouseholdId HouseholdId { get; }

    /// <summary>
    /// Gets the calendar year this budget covers.
    /// </summary>
    public int Year { get; }

    /// <summary>
    /// Gets the current status of this budget.
    /// </summary>
    public BudgetStatus Status { get; private set; }

    /// <summary>
    /// Gets the category nodes in this budget's category tree.
    /// </summary>
    public IReadOnlyCollection<CategoryNode> Categories => _categories.AsReadOnly();

    /// <summary>
    /// Gets the sum of all planned monthly amounts across all categories.
    /// </summary>
    public Money TotalPlannedMonthlyAmount =>
        _categories.Aggregate(
            Money.Zero(BudgetCurrency.Zar),
            (acc, node) => acc.Add(node.PlannedMonthlyAmount).GetValueOrDefault(acc));

    // IAuditable
    /// <inheritdoc />
    public UserId? CreatedBy { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? CreatedAt { get; private set; }

    /// <inheritdoc />
    public UserId? ModifiedBy { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedAt { get; private set; }

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

    // IHasDomainEvents
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
    /// Creates a new Budget in Draft status for the given household and year.
    /// </summary>
    /// <param name="householdId">The household that owns this budget.</param>
    /// <param name="year">The calendar year this budget covers. Must be a positive value.</param>
    /// <param name="auditStampFactory">Factory to record who created the budget.</param>
    /// <returns>Success with the new Budget; Failure with BudgetError if year is invalid.</returns>
    public static Result<Budget, BudgetError> Create(
        HouseholdId householdId,
        int year,
        IAuditStampFactory auditStampFactory)
    {
        if (year <= 0)
        {
            return new InvalidBudgetDataError("Year must be a positive value.");
        }

        Budget budget = new(
            id: BudgetId.NewId(),
            householdId: householdId,
            year: year,
            status: BudgetStatus.Draft,
            createdBy: null,
            createdAt: null,
            modifiedBy: null,
            modifiedAt: null,
            isDeleted: false,
            deletedAt: null,
            deletedBy: null);

        budget.Audit(auditStampFactory, AuditOperation.Create);
        budget.AddDomainEvent(new BudgetCreatedEvent(budget.Id, year));

        return budget;
    }

    /// <summary>
    /// Creates a new Draft budget for <paramref name="newYear"/> by deep-cloning all categories
    /// and planned amounts from <paramref name="sourceBudget"/>.
    /// </summary>
    /// <param name="sourceBudget">The budget to clone from.</param>
    /// <param name="newYear">The year of the new budget. Must be a positive value.</param>
    /// <param name="auditStampFactory">Factory to record who created the budget.</param>
    /// <returns>Success with the new cloned Budget; Failure with BudgetError if year is invalid.</returns>
    public static Result<Budget, BudgetError> CloneForYear(
        Budget sourceBudget,
        int newYear,
        IAuditStampFactory auditStampFactory)
    {
        if (newYear <= 0)
        {
            return new InvalidBudgetDataError("Year must be a positive value.");
        }

        Result<Budget, BudgetError> result = Create(sourceBudget.HouseholdId, newYear, auditStampFactory);
        if (result.IsFailure)
        {
            return result;
        }

        Budget newBudget = result.Value;

        // Build old-to-new ID mapping, processing nodes in topological order (parents before children)
        Dictionary<BudgetCategoryId, BudgetCategoryId> idMap = [];
        List<CategoryNode> remaining = [.. sourceBudget._categories.Where(n => !n.IsDeleted)];

        while (remaining.Count > 0)
        {
            List<CategoryNode> processable = remaining
                .Where(n => n.ParentId == null || idMap.ContainsKey(n.ParentId.Value))
                .ToList();

            foreach (CategoryNode node in processable)
            {
                BudgetCategoryId newId = BudgetCategoryId.NewId();
                idMap[node.Id] = newId;

                BudgetCategoryId? newParentId = node.ParentId.HasValue ? idMap[node.ParentId.Value] : null;

                newBudget._categories.Add(new CategoryNode(
                    id: newId,
                    name: node.Name,
                    parentId: newParentId,
                    canonicalCategoryId: node.CanonicalCategoryId,
                    budgetFlow: node.BudgetFlow,
                    attribution: node.Attribution,
                    description: node.Description,
                    incomeContributor: node.IncomeContributor,
                    responsiblePayer: node.ResponsiblePayer));

                remaining.Remove(node);
            }
        }

        return newBudget;
    }

    /// <summary>
    /// Adds a category to the budget's category tree.
    /// Category names must be unique within their sibling scope (same parent, non-deleted).
    /// </summary>
    public Result<CategoryNode, BudgetError> AddCategory(
        string name,
        BudgetFlow budgetFlow,
        BudgetCategoryId? parentId = null,
        string? description = null,
        Attribution? attribution = null,
        string? incomeContributor = null,
        string? responsiblePayer = null)
    {
        Result<CategoryName, BudgetError> nameResult = CategoryName.Create(name);
        if (nameResult.IsFailure)
        {
            return nameResult.Error;
        }

        CategoryName categoryName = nameResult.Value;

        // Validate parent if provided
        if (parentId is not null)
        {
            CategoryNode? parent = _categories.FirstOrDefault(c => c.Id == parentId.Value);
            if (parent is null)
            {
                return new CategoryNotFoundError(parentId.Value.ToString());
            }

            if (parent.IsDeleted)
            {
                return new DeletedParentError();
            }

            // Depth validation: parent must be a root (parent's parent must be null)
            if (parent.ParentId is not null)
            {
                return new CategoryDepthError("Cannot create a child of a child category. Maximum depth is 2 levels (root → child).");
            }
        }

        // Duplicate name check among non-deleted siblings
        bool isDuplicate = _categories
            .Where(c => c.ParentId == parentId && !c.IsDeleted)
            .Any(c => string.Equals(c.Name.Value, categoryName.Value, StringComparison.OrdinalIgnoreCase));

        if (isDuplicate)
        {
            return new DuplicateCategoryNameError(categoryName.Value);
        }

        CategoryNode node = new(
            id: BudgetCategoryId.NewId(),
            name: categoryName,
            parentId: parentId,
            canonicalCategoryId: CanonicalCategoryId.NewId(),
            budgetFlow: budgetFlow,
            attribution: attribution,
            description: description,
            incomeContributor: incomeContributor,
            responsiblePayer: responsiblePayer);

        _categories.Add(node);
        AddDomainEvent(new BudgetCategoryAddedEvent(Id, node.Id, node.Name.Value, parentId));

        return node;
    }

    /// <summary>
    /// Updates a category's mutable properties.
    /// </summary>
    public Result<CategoryNode, BudgetError> UpdateCategory(
        BudgetCategoryId categoryId,
        string name,
        BudgetFlow budgetFlow,
        Attribution? attribution = null,
        string? description = null,
        string? incomeContributor = null,
        string? responsiblePayer = null)
    {
        CategoryNode? node = _categories.FirstOrDefault(c => c.Id == categoryId);
        if (node is null)
        {
            return new CategoryNotFoundError(categoryId.ToString());
        }

        if (node.IsDeleted)
        {
            return new InvalidBudgetDataError("Cannot update a soft-deleted category.");
        }

        Result<CategoryName, BudgetError> nameResult = CategoryName.Create(name);
        if (nameResult.IsFailure)
        {
            return nameResult.Error;
        }

        CategoryName categoryName = nameResult.Value;

        // Duplicate name check among non-deleted siblings (exclude self)
        bool isDuplicate = _categories
            .Where(c => c.ParentId == node.ParentId && !c.IsDeleted && c.Id != categoryId)
            .Any(c => string.Equals(c.Name.Value, categoryName.Value, StringComparison.OrdinalIgnoreCase));

        if (isDuplicate)
        {
            return new DuplicateCategoryNameError(categoryName.Value);
        }

        node.Update(categoryName, budgetFlow, attribution, description, incomeContributor, responsiblePayer);
        AddDomainEvent(new CategoryUpdatedEvent(Id, categoryId));

        return node;
    }

    /// <summary>
    /// Changes a category's parent. Pass null to promote to root.
    /// </summary>
    public Result<CategoryNode, BudgetError> ReparentCategory(
        BudgetCategoryId categoryId,
        BudgetCategoryId? newParentId)
    {
        CategoryNode? node = _categories.FirstOrDefault(c => c.Id == categoryId);
        if (node is null)
        {
            return new CategoryNotFoundError(categoryId.ToString());
        }

        if (node.IsDeleted)
        {
            return new InvalidBudgetDataError("Cannot reparent a soft-deleted category.");
        }

        if (newParentId is not null)
        {
            CategoryNode? newParent = _categories.FirstOrDefault(c => c.Id == newParentId.Value);
            if (newParent is null)
            {
                return new CategoryNotFoundError(newParentId.Value.ToString());
            }

            if (newParent.IsDeleted)
            {
                return new DeletedParentError();
            }

            // Depth: new parent must be a root
            if (newParent.ParentId is not null)
            {
                return new CategoryDepthError("Cannot reparent under a child category. Maximum depth is 2 levels (root → child).");
            }

            // Cannot reparent to self
            if (newParentId.Value == categoryId)
            {
                return new InvalidBudgetDataError("Cannot reparent a category under itself.");
            }
        }

        // If the node being reparented is currently a root and has children,
        // and new parent is not null, then the children would become depth 3 — not allowed.
        if (newParentId is not null)
        {
            bool hasChildren = _categories.Any(c => c.ParentId == categoryId && !c.IsDeleted);
            if (hasChildren)
            {
                return new CategoryDepthError("Cannot reparent a root category with children under another category. This would exceed the 2-level depth limit.");
            }
        }

        // Duplicate name check in new sibling scope
        bool isDuplicate = _categories
            .Where(c => c.ParentId == newParentId && !c.IsDeleted && c.Id != categoryId)
            .Any(c => string.Equals(c.Name.Value, node.Name.Value, StringComparison.OrdinalIgnoreCase));

        if (isDuplicate)
        {
            return new DuplicateCategoryNameError(node.Name.Value);
        }

        BudgetCategoryId? oldParentId = node.ParentId;
        node.SetParent(newParentId);
        AddDomainEvent(new CategoryReparentedEvent(Id, categoryId, oldParentId, newParentId));

        return node;
    }

    /// <summary>
    /// Soft-deletes a category. Cascades to all children.
    /// </summary>
    public UnitResult<BudgetError> SoftDeleteCategory(
        BudgetCategoryId categoryId,
        ISoftDeleteStampFactory softDeleteStampFactory)
    {
        CategoryNode? node = _categories.FirstOrDefault(c => c.Id == categoryId);
        if (node is null)
        {
            return new CategoryNotFoundError(categoryId.ToString());
        }

        if (node.IsDeleted)
        {
            return UnitResult.Success<BudgetError>();
        }

        // Delete the node
        node.Delete(softDeleteStampFactory);

        // Cascade to children
        List<CategoryNode> children = _categories
            .Where(c => c.ParentId == categoryId && !c.IsDeleted)
            .ToList();

        foreach (CategoryNode child in children)
        {
            child.Delete(softDeleteStampFactory);
        }

        AddDomainEvent(new CategorySoftDeletedEvent(Id, categoryId));
        return UnitResult.Success<BudgetError>();
    }

    /// <summary>
    /// Restores a soft-deleted category. Cascades restoration to children.
    /// </summary>
    public UnitResult<BudgetError> RestoreCategory(BudgetCategoryId categoryId)
    {
        CategoryNode? node = _categories.FirstOrDefault(c => c.Id == categoryId);
        if (node is null)
        {
            return new CategoryNotFoundError(categoryId.ToString());
        }

        if (!node.IsDeleted)
        {
            return UnitResult.Success<BudgetError>();
        }

        // If this is a child, its parent must be active
        if (node.ParentId is not null)
        {
            CategoryNode? parent = _categories.FirstOrDefault(c => c.Id == node.ParentId.Value);
            if (parent is not null && parent.IsDeleted)
            {
                return new InvalidBudgetDataError("Cannot restore a child category while its parent is still deleted.");
            }
        }

        // Restore the node
        node.Restore();

        // Cascade restoration to children
        List<CategoryNode> deletedChildren = _categories
            .Where(c => c.ParentId == categoryId && c.IsDeleted)
            .ToList();

        foreach (CategoryNode child in deletedChildren)
        {
            child.Restore();
        }

        AddDomainEvent(new CategoryRestoredEvent(Id, categoryId));
        return UnitResult.Success<BudgetError>();
    }

    /// <summary>
    /// Activates this budget, making it the live plan for the year.
    /// Budget must be in Draft status.
    /// </summary>
    /// <returns>Success if activated; Failure with BudgetError if preconditions are not met.</returns>
    public UnitResult<BudgetError> Activate()
    {
        if (Status != BudgetStatus.Draft)
        {
            return new InvalidBudgetStatusError("Activate", Status.ToString());
        }

        Status = BudgetStatus.Active;
        AddDomainEvent(new BudgetActivatedEvent(Id));
        return UnitResult.Success<BudgetError>();
    }

    /// <summary>
    /// Closes this budget. Only an Active budget can be closed.
    /// </summary>
    /// <returns>Success if closed; Failure with BudgetError if budget is not Active.</returns>
    public UnitResult<BudgetError> Close()
    {
        if (Status != BudgetStatus.Active)
        {
            return new InvalidBudgetStatusError("Close", Status.ToString());
        }

        Status = BudgetStatus.Closed;
        AddDomainEvent(new BudgetClosedEvent(Id));
        return UnitResult.Success<BudgetError>();
    }
}
