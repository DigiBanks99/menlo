namespace Menlo.Lib.Common.Abstractions;

/// <summary>
/// Marker interface for aggregate roots in domain-driven design.
/// Aggregate roots are the entry points for all operations on an aggregate.
/// Only aggregate roots should be accessed directly from repositories.
/// </summary>
/// <typeparam name="TId">The type of the aggregate root's identifier.</typeparam>
public interface IAggregateRoot<out TId> : IEntity<TId>
{
}
