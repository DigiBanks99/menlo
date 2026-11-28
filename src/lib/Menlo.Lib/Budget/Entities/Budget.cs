using CSharpFunctionalExtensions;
using Menlo.Lib.Budget.Errors;
using Menlo.Lib.Budget.Events;
using Menlo.Lib.Budget.ValueObjects;
using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Common.Enums;
using Menlo.Lib.Common.ValueObjects;

namespace Menlo.Lib.Budget.Entities;

/// <summary>
/// Aggregate root for a household's budget: its committed statement, for one calendar year (January to December), of
/// what it expects to earn and spend. There is one budget per year, and it has no draft state: a budget is planned
/// from the moment it's made.
/// </summary>
public sealed class Budget : IAggregateRoot<BudgetId>, IHasDomainEvents, IAuditable
{
    /// <summary>The earliest year a budget can cover.</summary>
    public const int MinYear = 1;

    /// <summary>The latest year a budget can cover.</summary>
    public const int MaxYear = 9999;

    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// Private constructor for EF Core hydration.
    /// </summary>
    private Budget(
        BudgetId id,
        int year,
        UserId? createdBy,
        DateTimeOffset? createdAt,
        UserId? modifiedBy,
        DateTimeOffset? modifiedAt)
    {
        Id = id;
        Year = year;
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
    /// Gets the calendar year this budget covers.
    /// </summary>
    public int Year { get; }

    /// <inheritdoc />
    public UserId? CreatedBy { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? CreatedAt { get; private set; }

    /// <inheritdoc />
    public UserId? ModifiedBy { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedAt { get; private set; }

    /// <inheritdoc />
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Creates the budget for a calendar year. Whether the year already has one is checked where budgets are stored,
    /// since one budget can't see the others.
    /// </summary>
    /// <param name="year">The calendar year the budget covers.</param>
    /// <returns>The new budget, or why it can't be created.</returns>
    public static Result<Budget, BudgetError> Create(int year)
    {
        if (year is < MinYear or > MaxYear)
        {
            return new InvalidBudgetYearError(year);
        }

        Budget budget = new(
            id: BudgetId.NewId(),
            year: year,
            createdBy: null,
            createdAt: null,
            modifiedBy: null,
            modifiedAt: null);

        budget.AddDomainEvent(new BudgetCreatedEvent(budget.Id, year));

        return budget;
    }

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
}
