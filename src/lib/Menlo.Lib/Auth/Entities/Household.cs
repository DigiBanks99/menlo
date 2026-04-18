using CSharpFunctionalExtensions;
using Menlo.Lib.Auth.Errors;
using Menlo.Lib.Auth.Events;
using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Common.Enums;
using Menlo.Lib.Common.ValueObjects;

namespace Menlo.Lib.Auth.Entities;

/// <summary>
/// Aggregate root representing a household that groups users together.
/// </summary>
public sealed class Household : IAggregateRoot<HouseholdId>, IHasDomainEvents, IAuditable, ISoftDeletable
{
    private readonly List<IDomainEvent> _domainEvents = [];

    private Household(
        HouseholdId id,
        string name,
        UserId? createdBy,
        DateTimeOffset? createdAt,
        UserId? modifiedBy,
        DateTimeOffset? modifiedAt,
        bool isDeleted,
        DateTimeOffset? deletedAt,
        UserId? deletedBy)
    {
        Id = id;
        Name = name;
        CreatedBy = createdBy;
        CreatedAt = createdAt;
        ModifiedBy = modifiedBy;
        ModifiedAt = modifiedAt;
        IsDeleted = isDeleted;
        DeletedAt = deletedAt;
        DeletedBy = deletedBy;
    }

    /// <summary>
    /// Gets the unique identifier for this household.
    /// </summary>
    public HouseholdId Id { get; }

    /// <summary>
    /// Gets the household name.
    /// </summary>
    public string Name { get; }

    // IAuditable implementation
    /// <inheritdoc />
    public UserId? CreatedBy { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? CreatedAt { get; private set; }

    /// <inheritdoc />
    public UserId? ModifiedBy { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedAt { get; private set; }

    // ISoftDeletable implementation
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
    /// Factory method to create a new Household.
    /// </summary>
    /// <param name="name">The household name.</param>
    /// <returns>A Result containing the new Household or an error.</returns>
    public static Result<Household, HouseholdError> Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return new InvalidHouseholdDataError("Household name cannot be empty.");
        }

        Household household = new(
            id: HouseholdId.NewId(),
            name: name.Trim(),
            createdBy: null,
            createdAt: null,
            modifiedBy: null,
            modifiedAt: null,
            isDeleted: false,
            deletedAt: null,
            deletedBy: null);

        household.AddDomainEvent(new HouseholdCreatedEvent(household.Id, household.Name));

        return household;
    }
}
