using Menlo.Lib.Budget.ValueObjects;
using Menlo.Lib.Common.Abstractions;

namespace Menlo.Lib.Budget.Events;

/// <summary>
/// Domain event raised when a rental unit is renamed. A name is a label: renaming never changes the unit's identity.
/// </summary>
/// <param name="RentalUnitId">The ID of the rental unit.</param>
/// <param name="OldName">The name it had.</param>
/// <param name="NewName">The name it has now.</param>
public sealed record RentalUnitRenamedEvent(RentalUnitId RentalUnitId, string OldName, string NewName) : IDomainEvent;
