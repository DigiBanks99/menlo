# Money API Reference

This document provides a reference for the `Money` value object, a core primitive for handling monetary values in the Menlo application. Use it for every monetary value instead of a raw `decimal`, so currency travels with the amount and rounding is consistent.

**Namespace:** `Menlo.Lib.Common.ValueObjects`

## `Money` Struct

Represents a monetary value with currency. It is an immutable `readonly record struct` that ensures precision and currency safety.

ZAR is the household's currency. There is no currency conversion and no exchange-rate service: operations on different currencies fail rather than convert.

### Properties

- `decimal Amount { get; init; }`: The monetary amount, rounded to 2 decimal places.
- `string Currency { get; init; }`: The currency code (e.g., "ZAR", "USD"). Always uppercase.

### Rounding

Every `Money` the type creates, including the results of `Multiply` and `Divide`, rounds its amount to 2 decimal places with `MidpointRounding.ToEven` (banker's rounding). For example, `Create(10.005m, "ZAR")` gives 10.00 and `Create(10.015m, "ZAR")` gives 10.02. The currency is converted to upper case.

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

Splitting an amount by plain division loses or creates cents: R100.00 / 3 is R33.33 three times, which adds up to R99.99. Allocation works in whole cents so the parts always add up to the original amount. Splitting a bill between people, or a yearly amount into months, uses allocation rather than `Divide`.

Allocates Money into equal parts using the Penny Allocation pattern. Distributes remainder cents to the first parts, one each.

```csharp
public Result<IReadOnlyList<Money>, Error> Allocate(int parts)
```

- **Returns:** `Success` with list of Money parts; `Failure` with `InvalidAllocationError` if parts <= 0.
- **Example:** R100.00 in 3 parts gives R33.34, R33.33, R33.33.

#### `Allocate (Ratios)`

Allocates Money according to specified ratios.

```csharp
public Result<IReadOnlyList<Money>, Error> Allocate(params int[] ratios)
```

- **Returns:** `Success` with list of Money parts; `Failure` with `InvalidAllocationError` if no ratios are given, any ratio is negative, or they sum to zero.
- Every part except the last is its ratio's share, rounded down to the cent. The last part takes whatever is left, so it absorbs all the remainder cents.
- **Example:** R100.00 with ratios 30 and 70 gives R30.00 and R70.00.

### Comparison

Implements `IComparable<Money>`.

- `CompareTo(Money other)`: Throws `ArgumentException` if currencies do not match.
- Operators: `<`, `<=`, `>`, `>=` supported.
- Equality: `==`, `!=` supported (checks both Amount and Currency).

Comparing different currencies throws rather than returning a `Result`, so check that the currencies match before you compare.

## Persistence

EF Core maps `Money` as a complex property: an amount column and a currency column. See [Persistence](persistence.md#column-conventions).

## Error Codes

| Code        | Error Type               | Description                                                      |
| ----------- | ------------------------ | ---------------------------------------------------------------- |
| `MONEY_000` | `EmptyCurrencyError`     | Currency code cannot be null or empty.                           |
| `MONEY_001` | `CurrencyMismatchError`  | Operation requires matching currencies (e.g. adding ZAR to USD). |
| `MONEY_002` | `DivisionByZeroError`    | Cannot divide money by zero.                                     |
| `MONEY_003` | `InvalidAllocationError` | Allocation parameters are invalid (e.g. negative ratios).        |
