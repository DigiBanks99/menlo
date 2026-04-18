using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Common.ValueObjects;

namespace Menlo.Lib.Auth.Events;

/// <summary>
/// Domain event raised when a household is created.
/// </summary>
/// <param name="HouseholdId">The ID of the created household.</param>
/// <param name="Name">The name of the created household.</param>
public readonly record struct HouseholdCreatedEvent(HouseholdId HouseholdId, string Name) : IDomainEvent;
