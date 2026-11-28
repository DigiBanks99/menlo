using Menlo.Lib.Budget.ValueObjects;
using Menlo.Lib.Common.Abstractions;

namespace Menlo.Lib.Budget.Events;

/// <summary>
/// Domain event raised when a rental unit is added to the household.
/// </summary>
/// <param name="RentalUnitId">The ID of the new rental unit.</param>
/// <param name="Name">Its name.</param>
public sealed record RentalUnitCreatedEvent(RentalUnitId RentalUnitId, string Name) : IDomainEvent;
