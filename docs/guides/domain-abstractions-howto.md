# How-to: Use Domain Abstractions

This guide provides practical examples for common tasks when working with the Menlo Domain Abstractions.

## How to Create a Strongly-Typed ID

Use a `readonly record struct` to define an ID. This provides value equality and immutability with zero allocation overhead.

```csharp
public readonly record struct ProductId(Guid Value);
```

## How to Raise a Domain Event

To raise a domain event from within an Aggregate Root, use the `AddDomainEvent` method.

```csharp
public void Publish()
{
    this.IsPublished = true;
    this.AddDomainEvent(new ProductPublished(this.Id));
}
```

**Note:** The `AddDomainEvent` method is generic (`AddDomainEvent<T>(T evt)`) to avoid boxing struct-based events.

## How to Implement Auditing

Implement the `IAuditable` interface on your entity.

```csharp
public class Product : IEntity<ProductId>, IAuditable
{
    public ProductId Id { get; private set; }
    
    // IAuditable implementation
    public UserId? CreatedBy { get; private set; }
    public DateTimeOffset? CreatedAt { get; private set; }
    public UserId? ModifiedBy { get; private set; }
    public DateTimeOffset? ModifiedAt { get; private set; }

    public void Audit(IAuditStampFactory factory, AuditOperation operation)
    {
        var stamp = factory.CreateStamp();
        if (operation == AuditOperation.Create)
        {
            CreatedBy = stamp.ActorId;
            CreatedAt = stamp.Timestamp;
        }

        ModifiedBy = stamp.ActorId;
        ModifiedAt = stamp.Timestamp;
    }
}
```

Typically, the `Audit` method is called by the repository or a domain service using an `IAuditStampFactory` before persistence.

## How to Define a Domain Error

Create a class that derives from `Error` with the code and message you desire. Add the additional properties you require.

```csharp
public class ProductError(string code, string message) : Error(code, message);

public class ProductNotFoundError(ProductId productId)
   : ProductError("Product.NotFound", $"The product {productId} was not found")
{
  public ProductId ProductId { get; } = productId;
}
```

Use these errors with the Result pattern:

```csharp
public Result<Product, ProductError> GetProduct(ProductId id)
{
    // ... logic
    if (product == null)
    {
        return new ProductNotFoundError(id);
    }

    return product;
}
```

## Further Reading

- [Tutorial: Add a Domain vertical slice](../tutorials/domain-abstractions-tutorial.md)
- [Reference: Domain Abstractions](../reference/domain-abstractions-api.md)
