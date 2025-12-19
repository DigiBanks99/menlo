# Tutorial: Building a Domain Model with Menlo Abstractions

This tutorial guides you through creating a rich domain model using the Menlo project's core abstractions. We will build a simple `Budget` aggregate to demonstrate the concepts.

- [Tutorial: Building a Domain Model with Menlo Abstractions](#tutorial-building-a-domain-model-with-menlo-abstractions)
  - [Prerequisites](#prerequisites)
  - [Step 1: Define the Identifier](#step-1-define-the-identifier)
  - [Step 2: Define Domain Events](#step-2-define-domain-events)
  - [Step 3: Create the Aggregate Root](#step-3-create-the-aggregate-root)
  - [Step 4: Using the Aggregate](#step-4-using-the-aggregate)
  - [Summary](#summary)
  - [Further Reading](#further-reading)

## Prerequisites

- Understanding of C# and .NET.
- Familiarity with Domain-Driven Design (DDD) concepts.
- The `Menlo.Lib` project referenced in your solution.

## Step 1: Define the Identifier

First, we define a strongly-typed identifier for our Aggregate Root. This prevents "primitive obsession" (using raw GUIDs or strings) and ensures type safety.

```csharp
using Menlo.Lib.Common.ValueObjects;

public readonly record struct BudgetId(Guid Value)
{
    public static BudgetId New() => new(Guid.NewGuid());
    public static BudgetId Empty => new(Guid.Empty);
}
```

## Step 2: Define Domain Events

Define the events that can happen within your domain. These should be immutable records implementing `IDomainEvent`.

```csharp
using Menlo.Lib.Common.Abstractions;

public record BudgetCreated(BudgetId Id, string Name, UserId Owner) : IDomainEvent;
public record BudgetLimitUpdated(BudgetId Id, decimal NewLimit) : IDomainEvent;
```

## Step 3: Create the Aggregate Root

Now, create the `Budget` class implementing `IAggregateRoot<BudgetId>`.

```csharp
using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Common.ValueObjects;

public class Budget : IAggregateRoot<BudgetId>
{
    // Private collection for domain events
    private readonly List<IDomainEvent> _domainEvents = new();

    public BudgetId Id { get; private init; }
    public string Name { get; private init; }
    public decimal Limit { get; private init; }
    public UserId Owner { get; private init; }

    private Budget(
      BudgetId id,
      string name,
      decimal limit,
      UserId owner)
    {
      Id = id;
      Name = name;
      Limit = limit;
      Owner = owner;
    }

    // Implement IHasDomainEvents
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void AddDomainEvent<TEvent>(TEvent domainEvent) where TEvent : IDomainEvent
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    // Factory method for creation
    public static Budget Create(string name, decimal limit, UserId owner)
    {
        var budget = new Budget
        {
            Id = BudgetId.New(),
            Name = name,
            Limit = limit,
            Owner = owner
        };

        // Raise domain event
        budget.AddDomainEvent(new BudgetCreated(budget.Id, budget.Name, budget.Owner));

        return budget;
    }

    // Domain behaviour
    public void UpdateLimit(decimal newLimit)
    {
        if (newLimit < 0)
        {
            // In a real scenario, return a Result.Failure here
            throw new ArgumentException("Limit cannot be negative");
        }

        Limit = newLimit;
        AddDomainEvent(new BudgetLimitUpdated(Id, newLimit));
    }
}
```

## Step 4: Using the Aggregate

Here is how you would use the `Budget` aggregate in a service or handler.

```csharp
public void CreateBudgetExample()
{
    var ownerId = UserService.GetCurrent();
    var budget = Budget.Create("Family Budget", 1000m, ownerId);

    // Access properties
    logger.BudgetCreated("Budget {BudgetName} created with limit {Limit}", budget.Name, budget.Limit);

    // Check events
    foreach (var evt in budget.DomainEvents)
    {
        if (evt is BudgetCreated created)
        {
            logger.DomainEventRecorded($"Event: Budget {EventIdentifier} created", evt.Value.Id);
        }
    }
}
```

## Summary

In this tutorial, you learned how to:

1. Create a strongly-typed ID using `record struct`.
2. Define domain events using `IDomainEvent`.
3. Implement an Aggregate Root using `IAggregateRoot<T>`.
4. Raise and manage domain events.

This pattern ensures your domain logic is encapsulated, type-safe, and easy to test.

## Further Reading

- [How to use Domain Abstractions](../guides/domain-abstractions-howto.md)
- [Domain Abstractions Reference](../reference/domain-abstractions-api.md)
