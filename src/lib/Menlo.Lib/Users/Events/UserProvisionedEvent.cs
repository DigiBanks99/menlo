using Menlo.Lib.Common.Abstractions;

namespace Menlo.Lib.Users.Events;

public sealed record UserProvisionedEvent(
    Guid UserId,
    string Email,
    string DisplayName,
    DateTime Timestamp) : IDomainEvent;
