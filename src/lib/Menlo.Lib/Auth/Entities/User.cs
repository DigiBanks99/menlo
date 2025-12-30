using CSharpFunctionalExtensions;
using Menlo.Lib.Auth.Errors;
using Menlo.Lib.Auth.Events;
using Menlo.Lib.Auth.ValueObjects;
using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Common.Enums;
using Menlo.Lib.Common.ValueObjects;

namespace Menlo.Lib.Auth.Entities;

/// <summary>
/// Aggregate root representing a system user linked to an external identity provider.
/// </summary>
public sealed class User : IAggregateRoot<UserId>, IHasDomainEvents, IAuditable
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// Private constructor for EF Core hydration.
    /// EF Core can use this constructor to set all properties via constructor binding.
    /// </summary>
    private User(
        UserId id,
        ExternalUserId externalId,
        string email,
        string displayName,
        DateTimeOffset? lastLoginAt,
        UserId? createdBy,
        DateTimeOffset? createdAt,
        UserId? modifiedBy,
        DateTimeOffset? modifiedAt)
    {
        Id = id;
        ExternalId = externalId;
        Email = email;
        DisplayName = displayName;
        LastLoginAt = lastLoginAt;
        CreatedBy = createdBy;
        CreatedAt = createdAt;
        ModifiedBy = modifiedBy;
        ModifiedAt = modifiedAt;
    }

    /// <summary>
    /// Gets the unique identifier for this user.
    /// </summary>
    public UserId Id { get; }

    /// <summary>
    /// Gets the external identity provider's user identifier.
    /// </summary>
    public ExternalUserId ExternalId { get; }

    /// <summary>
    /// Gets the user's email address.
    /// </summary>
    public string Email { get; }

    /// <summary>
    /// Gets the user's display name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets when the user last logged in.
    /// </summary>
    public DateTimeOffset? LastLoginAt { get; private set; }

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
    /// Factory method to create a new User.
    /// </summary>
    /// <param name="externalId">The external identity provider's user identifier.</param>
    /// <param name="email">The user's email address.</param>
    /// <param name="displayName">The user's display name.</param>
    /// <returns>A Result containing the new User or an error.</returns>
    public static Result<User, AuthError> Create(ExternalUserId externalId, string email, string displayName)
    {
        if (string.IsNullOrWhiteSpace(externalId.Value))
        {
            return new InvalidUserDataError("External ID cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return new InvalidUserDataError("Email cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            return new InvalidUserDataError("Display name cannot be empty.");
        }

        User user = new(
            id: UserId.NewId(),
            externalId: externalId,
            email: email.Trim(),
            displayName: displayName.Trim(),
            lastLoginAt: DateTimeOffset.UtcNow,
            createdBy: null,
            createdAt: null,
            modifiedBy: null,
            modifiedAt: null);

        return user;
    }

    /// <summary>
    /// Records a login event for this user.
    /// </summary>
    /// <param name="timestamp">When the login occurred.</param>
    public void RecordLogin(DateTimeOffset timestamp)
    {
        LastLoginAt = timestamp;
        AddDomainEvent(new UserLoggedInEvent(Id, timestamp));
    }
}
