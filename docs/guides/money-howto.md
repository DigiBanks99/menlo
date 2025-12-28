# How-to: Work with Money

This guide provides practical examples for working with the `Money` value object in the Menlo application. The `Money` struct is a core primitive designed to handle monetary values safely, preventing common errors like precision loss and currency mismatches.

## How to Create Money Instances

Always use the `Money.Create` factory method or `Money.Zero` to create instances. The constructor is private to enforce validation.

### Creating a Valid Amount

```csharp
using Menlo.Lib.Common.ValueObjects;

// Returns Result<Money, Error>
var priceResult = Money.Create(199.99m, "ZAR");

if (priceResult.IsSuccess)
{
    Money price = priceResult.Value;
    // Use price...
}
```

### Creating a Zero Amount

```csharp
// Useful for initialization
Money total = Money.Zero("ZAR");
```

## How to Perform Arithmetic

Arithmetic operations return a `Result<Money, Error>` to handle potential errors like currency mismatches or division by zero.

### Adding and Subtracting

```csharp
var wallet = Money.Create(100m, "ZAR").Value;
var cost = Money.Create(25.50m, "ZAR").Value;

// Add
var sumResult = wallet.Add(cost); // 125.50 ZAR

// Subtract
var remainingResult = wallet.Subtract(cost); // 74.50 ZAR
```

### Multiplying and Dividing

Multiplication returns `Money` directly (no validation needed), while division returns a `Result` (checks for zero divisor).

```csharp
var price = Money.Create(10.00m, "ZAR").Value;

// Multiply
Money total = price.Multiply(3); // 30.00 ZAR

// Divide
var splitResult = price.Divide(2); // 5.00 ZAR
```

## How to Allocate Money

Use the `Allocate` method to split money without losing cents. This implements the "Penny Allocation" pattern.

### Equal Allocation

```csharp
var bill = Money.Create(100.00m, "ZAR").Value;

// Split into 3 equal parts
var partsResult = bill.Allocate(3);

// Result: [33.34, 33.33, 33.33]
// Sum is exactly 100.00
```

### Ratio-Based Allocation

```csharp
var budget = Money.Create(1000.00m, "ZAR").Value;

// Allocate 70% to needs, 20% to savings, 10% to wants (7:2:1)
var partsResult = budget.Allocate(7, 2, 1);

// Result: [700.00, 200.00, 100.00]
```

## How to Handle Currency Mismatches

Operations between different currencies will return a `CurrencyMismatchError`.

```csharp
var zar = Money.Create(100m, "ZAR").Value;
var usd = Money.Create(100m, "USD").Value;

var result = zar.Add(usd);

if (result.IsFailure)
{
    // result.Error is CurrencyMismatchError
    // Message: "Currency mismatch: expected 'ZAR' but got 'USD'"
}
```

## How to Compare Money

`Money` implements `IComparable<Money>` and supports standard comparison operators.

```csharp
var a = Money.Create(10m, "ZAR").Value;
var b = Money.Create(20m, "ZAR").Value;

if (a < b) { /* ... */ }
if (a == b) { /* ... */ }
```

**Note:** Comparing different currencies throws an `ArgumentException` to prevent logical errors in sorting or filtering.
