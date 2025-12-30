using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Common.ValueObjects;

namespace Menlo.Lib.Auth.Events;

/// <summary>
/// Domain event raised when a user successfully logs in.
/// </summary>
/// <param name="UserId">The ID of the user who logged in.</param>
/// <param name="Timestamp">When the login occurred.</param>
public readonly record struct UserLoggedInEvent(UserId UserId, DateTimeOffset Timestamp) : IDomainEvent;
