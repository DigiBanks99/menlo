using Menlo.Lib.Common.Abstractions;
using Shouldly;

namespace Menlo.Lib.Tests.Common.Abstractions;

/// <summary>
/// Tests for Domain Events functionality.
/// TC-04: Domain Events Collection
/// </summary>
public sealed class DomainEventsTests
{
    private readonly record struct TestDomainEvent(string Message) : IDomainEvent;

    private sealed class TestAggregate : IHasDomainEvents
    {
        private readonly List<IDomainEvent> _domainEvents = new();

        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        public void AddDomainEvent<TEvent>(TEvent domainEvent) where TEvent : IDomainEvent
        {
            ArgumentNullException.ThrowIfNull(domainEvent);
            _domainEvents.Add(domainEvent);
        }

        public void ClearDomainEvents()
        {
            _domainEvents.Clear();
        }
    }

    [Fact]
    public void GivenAggregate_WhenAddingTwoDomainEvents_AndClearingEvents()
    {
        // Arrange
        TestAggregate aggregate = new();
        TestDomainEvent event1 = new("First Event");
        TestDomainEvent event2 = new("Second Event");

        // Act - Add two events
        aggregate.AddDomainEvent(event1);
        aggregate.AddDomainEvent(event2);

        // Assert
        ItShouldContainTwoEvents(aggregate, event1, event2);

        // Act - Clear events
        aggregate.ClearDomainEvents();

        // Assert
        ItShouldBeEmpty(aggregate);
    }

    private static void ItShouldContainTwoEvents(TestAggregate aggregate, TestDomainEvent event1, TestDomainEvent event2)
    {
        aggregate.DomainEvents.Count.ShouldBe(2);
        aggregate.DomainEvents.ShouldContain(event1);
        aggregate.DomainEvents.ShouldContain(event2);
    }

    private static void ItShouldBeEmpty(TestAggregate aggregate)
    {
        aggregate.DomainEvents.Count.ShouldBe(0);
    }

    [Fact]
    public void GivenAggregate_WhenNoDomainEvents()
    {
        // Arrange
        TestAggregate aggregate = new();

        // Act & Assert
        ItShouldHaveEmptyCollection(aggregate);
    }

    private static void ItShouldHaveEmptyCollection(TestAggregate aggregate)
    {
        aggregate.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void GivenAggregate_WhenAddingSingleDomainEvent()
    {
        // Arrange
        TestAggregate aggregate = new();
        TestDomainEvent domainEvent = new("Test Event");

        // Act
        aggregate.AddDomainEvent(domainEvent);

        // Assert
        ItShouldContainTheEvent(aggregate, domainEvent);
    }

    private static void ItShouldContainTheEvent(TestAggregate aggregate, TestDomainEvent domainEvent)
    {
        aggregate.DomainEvents.Count.ShouldBe(1);
        aggregate.DomainEvents.First().ShouldBe(domainEvent);
    }

    [Fact]
    public void GivenDomainEvent_WhenUsingStructBasedEvent()
    {
        // Arrange
        TestAggregate aggregate = new();
        TestDomainEvent structEvent = new("Struct Event");

        // Act
        aggregate.AddDomainEvent(structEvent);

        // Assert
        ItShouldStoreTheStructEvent(aggregate);
    }

    private static void ItShouldStoreTheStructEvent(TestAggregate aggregate)
    {
        // The generic constraint ensures the struct is added without boxing during the Add call
        // (though it will be boxed when stored in List<IDomainEvent>)
        aggregate.DomainEvents.Count.ShouldBe(1);
        ((TestDomainEvent)aggregate.DomainEvents.First()).Message.ShouldBe("Struct Event");
    }
}
