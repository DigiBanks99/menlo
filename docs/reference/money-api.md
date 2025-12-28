# Money API Reference

This document provides a reference for the `Money` value object, a core primitive for handling monetary values in the Menlo application.

**Namespace:** `Menlo.Lib.Common.ValueObjects`

## `Money` Struct

Represents a monetary value with currency. It is an immutable `readonly record struct` that ensures precision and currency safety.

### Properties

- `decimal Amount { get; init; }`: The monetary amount with 2 decimal places precision.
- `string Currency { get; init; }`: The currency code (e.g., "ZAR", "USD"). Always uppercase.

### Factory Methods

#### `Create`

Creates a new Money instance with validation.

```csharp
public static Result<Money, Error> Create(decimal amount, string currency)
```

- **Returns:** `Success` with `Money` if valid; `Failure` with `EmptyCurrencyError` if currency is empty.

#### `Zero`

Creates a Money instance with zero amount in the specified currency.

```csharp
public static Money Zero(string currency)
```

### Arithmetic Operations

#### `Add`

Adds two Money values.

```csharp
public Result<Money, Error> Add(Money other)
```

- **Returns:** `Success` with the sum if currencies match; `Failure` with `CurrencyMismatchError` otherwise.

#### `Subtract`

Subtracts one Money value from another.

```csharp
public Result<Money, Error> Subtract(Money other)
```

- **Returns:** `Success` with the difference if currencies match; `Failure` with `CurrencyMismatchError` otherwise.

#### `Multiply`

Multiplies Money by a factor.

```csharp
public Money Multiply(decimal factor)
```

#### `Divide`

Divides Money by a divisor.

```csharp
public Result<Money, Error> Divide(decimal divisor)
```

- **Returns:** `Success` with the divided Money; `Failure` with `DivisionByZeroError` if divisor is zero.

### Allocation Operations

#### `Allocate (Equal)`

Allocates Money into equal parts using the Penny Allocation pattern. Distributes remainder cents to the first parts.

```csharp
public Result<IReadOnlyList<Money>, Error> Allocate(int parts)
```

- **Returns:** `Success` with list of Money parts; `Failure` with `InvalidAllocationError` if parts <= 0.

#### `Allocate (Ratios)`

Allocates Money according to specified ratios.

```csharp
public Result<IReadOnlyList<Money>, Error> Allocate(params int[] ratios)
```

- **Returns:** `Success` with list of Money parts; `Failure` with `InvalidAllocationError` if ratios are invalid.

### Comparison

Implements `IComparable<Money>`.

- `CompareTo(Money other)`: Throws `ArgumentException` if currencies do not match.
- Operators: `<`, `<=`, `>`, `>=` supported.
- Equality: `==`, `!=` supported (checks both Amount and Currency).

## Error Codes

| Code        | Error Type               | Description                                                      |
| ----------- | ------------------------ | ---------------------------------------------------------------- |
| `MONEY_000` | `EmptyCurrencyError`     | Currency code cannot be null or empty.                           |
| `MONEY_001` | `CurrencyMismatchError`  | Operation requires matching currencies (e.g. adding ZAR to USD). |
| `MONEY_002` | `DivisionByZeroError`    | Cannot divide money by zero.                                     |
| `MONEY_003` | `InvalidAllocationError` | Allocation parameters are invalid (e.g. negative ratios).        |
