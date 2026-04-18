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
        foreach (CategoryNode node in sourceBudget._categories)
        {
            newBudget._categories.Add(new CategoryNode(
                id: BudgetCategoryId.NewId(),
                name: node.Name,
                parentId: null,
                plannedMonthlyAmount: node.PlannedMonthlyAmount));
        }

        return newBudget;
    }

    /// <summary>
    /// Adds a category to the budget's category tree.
    /// Category names must be unique within their sibling scope (same parent).
    /// </summary>
    /// <param name="name">The category name.</param>
    /// <param name="parentId">The parent category ID, or null for a root category.</param>
    /// <returns>Success with the new CategoryNode; Failure with BudgetError if name is invalid or duplicate.</returns>
    public Result<CategoryNode, BudgetError> AddCategory(string name, BudgetCategoryId? parentId = null)
    {
        Result<CategoryName, BudgetError> nameResult = CategoryName.Create(name);
        if (nameResult.IsFailure)
        {
            return nameResult.Error;
        }

        CategoryName categoryName = nameResult.Value;

        bool isDuplicate = _categories
            .Where(c => c.ParentId == parentId)
            .Any(c => string.Equals(c.Name.Value, categoryName.Value, StringComparison.OrdinalIgnoreCase));

        if (isDuplicate)
        {
            return new DuplicateCategoryNameError(categoryName.Value);
        }

        CategoryNode node = new(
            id: BudgetCategoryId.NewId(),
            name: categoryName,
            parentId: parentId,
            plannedMonthlyAmount: Money.Zero(BudgetCurrency.Zar));

        _categories.Add(node);
        AddDomainEvent(new BudgetCategoryAddedEvent(Id, node.Id, node.Name.Value, parentId));

        return node;
    }

    /// <summary>
    /// Sets the default planned monthly amount for a category.
    /// </summary>
    /// <param name="categoryId">The category to update.</param>
    /// <param name="amount">The planned monthly amount. Must be non-negative.</param>
    /// <returns>Success if updated; Failure with BudgetError if category not found or amount is negative.</returns>
    public UnitResult<BudgetError> SetPlanned(BudgetCategoryId categoryId, Money amount)
    {
        if (amount.Amount < 0)
        {
            return new InvalidBudgetDataError("Planned amount cannot be negative.");
        }

        CategoryNode? node = _categories.FirstOrDefault(c => c.Id == categoryId);
        if (node is null)
        {
            return new InvalidBudgetDataError($"Category '{categoryId}' not found in this budget.");
        }

        node.SetPlannedAmount(amount);
        AddDomainEvent(new PlannedAmountSetEvent(Id, categoryId, amount));
        return UnitResult.Success<BudgetError>();
    }

    /// <summary>
    /// Activates this budget, making it the live plan for the year.
    /// At least one category must have a non-zero planned amount.
    /// </summary>
    /// <returns>Success if activated; Failure with BudgetError if preconditions are not met.</returns>
    public UnitResult<BudgetError> Activate()
    {
        if (Status != BudgetStatus.Draft)
        {
            return new InvalidBudgetStatusError("Activate", Status.ToString());
        }

        bool hasNonZeroAmount = _categories.Any(c => c.PlannedMonthlyAmount.Amount > 0);
        if (!hasNonZeroAmount)
        {
            return new BudgetActivationError("At least one category must have a non-zero planned amount.");
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
