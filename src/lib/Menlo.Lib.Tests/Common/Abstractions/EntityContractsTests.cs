using Menlo.Lib.Common.Abstractions;
using Shouldly;

namespace Menlo.Lib.Tests.Common.Abstractions;

/// <summary>
/// Tests for Entity and Aggregate Root contracts.
/// TC-02: Entity Contract
/// TC-03: Aggregate Root Marker
/// </summary>
public sealed class EntityContractsTests
{
    private readonly record struct TestEntityId(Guid Value);

    private sealed class TestEntity : IEntity<TestEntityId>
    {
        public TestEntityId Id { get; init; }
    }

    private sealed class TestAggregateRoot : IAggregateRoot<TestEntityId>
    {
        private readonly List<IDomainEvent> _domainEvents = new();

        public TestEntityId Id { get; init; }

        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        public void AddDomainEvent<TEvent>(TEvent domainEvent) where TEvent : IDomainEvent
        {
            _domainEvents.Add(domainEvent);
        }

        public void ClearDomainEvents()
        {
            _domainEvents.Clear();
        }
    }

    [Fact]
    public void GivenEntityImplementation_WhenAccessingId()
    {
        // Arrange
        TestEntityId expectedId = new(Guid.NewGuid());
        TestEntity entity = new() { Id = expectedId };

        // Act
        TestEntityId actualId = entity.Id;

        // Assert
        ItShouldExposeStronglyTypedId(actualId, expectedId);
    }

    private static void ItShouldExposeStronglyTypedId(TestEntityId actualId, TestEntityId expectedId)
    {
        actualId.ShouldBe(expectedId);
        actualId.Value.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void GivenAggregateRootImplementation_WhenCheckingInterfaces()
    {
        // Arrange
        TestAggregateRoot aggregate = new() { Id = new TestEntityId(Guid.NewGuid()) };

        // Act & Assert
        ItShouldImplementRequiredInterfaces(aggregate);
    }

    private static void ItShouldImplementRequiredInterfaces(TestAggregateRoot aggregate)
    {
        aggregate.ShouldBeAssignableTo<IEntity<TestEntityId>>();
        aggregate.ShouldBeAssignableTo<IAggregateRoot<TestEntityId>>();
    }

    [Fact]
    public void GivenAggregateRootImplementation_WhenAccessingId()
    {
        // Arrange
        TestEntityId expectedId = new(Guid.NewGuid());
        TestAggregateRoot aggregate = new() { Id = expectedId };

        // Act
        TestEntityId actualId = aggregate.Id;

        // Assert
        ItShouldExposeAggregateId(actualId, expectedId);
    }

    private static void ItShouldExposeAggregateId(TestEntityId actualId, TestEntityId expectedId)
    {
        actualId.ShouldBe(expectedId);
    }
}
