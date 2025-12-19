namespace Menlo.Lib.Common.Abstractions;

/// <summary>
/// Marker interface for all domain events.
/// All domain events must implement this interface to ensure type safety
/// and avoid boxing/unboxing overhead.
/// </summary>
public interface IDomainEvent
{
}
