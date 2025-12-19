namespace Menlo.Lib.Common.Abstractions;

/// <summary>
/// Interface for entities that can raise domain events.
/// Domain events represent significant business occurrences within the domain.
/// Uses <see cref="IDomainEvent"/> to avoid boxing and ensure type safety.
/// </summary>
public interface IHasDomainEvents
{
    /// <summary>
    /// Gets the collection of domain events raised by this entity.
    /// Events should be dispatched by the application layer after persistence.
    /// </summary>
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    /// <summary>
    /// Adds a domain event to be published after the current operation completes.
    /// The generic constraint ensures compile-time type safety and avoids boxing.
    /// </summary>
    /// <typeparam name="TEvent">The type of domain event. Must implement <see cref="IDomainEvent"/>.</typeparam>
    /// <param name="domainEvent">The domain event to add. Must not be null.</param>
    void AddDomainEvent<TEvent>(TEvent domainEvent) where TEvent : IDomainEvent;

    /// <summary>
    /// Clears all pending domain events.
    /// Typically called by the application layer after events have been dispatched.
    /// </summary>
    void ClearDomainEvents();
}
