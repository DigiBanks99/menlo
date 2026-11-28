using CSharpFunctionalExtensions;
using Menlo.Lib.Budget.Errors;
using Menlo.Lib.Budget.Events;
using Menlo.Lib.Budget.ValueObjects;
using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Common.Enums;
using Menlo.Lib.Common.ValueObjects;

namespace Menlo.Lib.Budget.Entities;

/// <summary>
/// Aggregate root for a rental unit: a part of the property that the household lets to a tenant. It belongs to the
/// household, not to a budget, so it carries across years. Its name is a label: renaming it is an edit that never
/// changes its identity.
/// </summary>
public sealed class RentalUnit : IAggregateRoot<RentalUnitId>, IHasDomainEvents, IAuditable
{
    /// <summary>The longest a rental unit's name can be.</summary>
    public const int MaxNameLength = 100;

    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// Private constructor for EF Core hydration.
    /// </summary>
    private RentalUnit(
        RentalUnitId id,
        string name,
        UserId? createdBy,
        DateTimeOffset? createdAt,
        UserId? modifiedBy,
        DateTimeOffset? modifiedAt)
    {
        Id = id;
        Name = name;
        CreatedBy = createdBy;
        CreatedAt = createdAt;
        ModifiedBy = modifiedBy;
        ModifiedAt = modifiedAt;
    }

    /// <summary>
    /// Gets the unique identifier for this rental unit.
    /// </summary>
    public RentalUnitId Id { get; }

    /// <summary>
    /// Gets the rental unit's name, a label.
    /// </summary>
    public string Name { get; private set; }

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
    /// Adds a rental unit to the household.
    /// </summary>
    /// <param name="name">Its name. Leading and trailing spaces are dropped.</param>
    /// <returns>The new rental unit, or why it can't be added.</returns>
    public static Result<RentalUnit, BudgetError> Create(string? name)
    {
        Result<string, BudgetError> validName = ValidName(name);
        if (validName.IsFailure)
        {
            return validName.Error;
        }

        RentalUnit unit = new(
            id: RentalUnitId.NewId(),
            name: validName.Value,
            createdBy: null,
            createdAt: null,
            modifiedBy: null,
            modifiedAt: null);

        unit.AddDomainEvent(new RentalUnitCreatedEvent(unit.Id, unit.Name));

        return unit;
    }

    /// <summary>
    /// Renames the rental unit. Renaming it to the name it already has changes nothing.
    /// </summary>
    /// <param name="name">Its new name. Leading and trailing spaces are dropped.</param>
    /// <returns>Success, or why it can't have that name.</returns>
    public UnitResult<BudgetError> Rename(string? name)
    {
        Result<string, BudgetError> validName = ValidName(name);
        if (validName.IsFailure)
        {
            return validName.Error;
        }

        if (validName.Value == Name)
        {
            return UnitResult.Success<BudgetError>();
        }

        string oldName = Name;
        Name = validName.Value;
        AddDomainEvent(new RentalUnitRenamedEvent(Id, oldName, Name));

        return UnitResult.Success<BudgetError>();
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

    private static Result<string, BudgetError> ValidName(string? name)
    {
        string? trimmed = name?.Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed.Length > MaxNameLength)
        {
            return new InvalidRentalUnitNameError(name);
        }

        return trimmed;
    }
}
