using CSharpFunctionalExtensions;
using Menlo.Lib.Common.Errors;
using Menlo.Lib.Common.Abstractions;

namespace Menlo.Lib.Common.ValueObjects;

/// <summary>
/// Represents a monetary value with currency.
/// Immutable value object that ensures precision and currency safety.
/// </summary>
public readonly record struct Money : IComparable<Money>
{
    /// <summary>
    /// Gets the monetary amount with 2 decimal places precision.
    /// </summary>
    public decimal Amount { get; init; }

    /// <summary>
    /// Gets the currency code (e.g., "ZAR", "USD"). Always uppercase.
    /// </summary>
    public string Currency { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Money"/> struct.
    /// This constructor is private and used only by EF Core for hydration.
    /// Use the <see cref="Create"/> factory method for all other construction.
    /// </summary>
    /// <param name="amount">The monetary amount.</param>
    /// <param name="currency">The currency code.</param>
    private Money(decimal amount, string currency)
    {
        Amount = Math.Round(amount, 2, MidpointRounding.ToEven);
        Currency = currency.ToUpperInvariant();
    }

    /// <summary>
    /// Creates a new Money instance with validation.
    /// </summary>
    /// <param name="amount">The monetary amount.</param>
    /// <param name="currency">The currency code.</param>
    /// <returns>Success with Money if valid; Failure with EmptyCurrencyError if currency is empty.</returns>
    public static Result<Money, Error> Create(decimal amount, string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            return new EmptyCurrencyError();
        }

        return new Money(amount, currency);
    }

    /// <summary>
    /// Creates a Money instance with zero amount in the specified currency.
    /// </summary>
    /// <param name="currency">The currency code.</param>
    /// <returns>Money with zero amount.</returns>
    public static Money Zero(string currency) => new(0, currency);

    /// <summary>
    /// Adds two Money values.
    /// </summary>
    /// <param name="other">The Money value to add.</param>
    /// <returns>Success with the sum if currencies match; Failure with CurrencyMismatchError otherwise.</returns>
    public Result<Money, Error> Add(Money other)
    {
        if (!Currency.Equals(other.Currency, StringComparison.Ordinal))
        {
            return new CurrencyMismatchError(Currency, other.Currency);
        }

        return new Money(Amount + other.Amount, Currency);
    }

    /// <summary>
    /// Subtracts one Money value from another.
    /// </summary>
    /// <param name="other">The Money value to subtract.</param>
    /// <returns>Success with the difference if currencies match; Failure with CurrencyMismatchError otherwise.</returns>
    public Result<Money, Error> Subtract(Money other)
    {
        if (!Currency.Equals(other.Currency, StringComparison.Ordinal))
        {
            return new CurrencyMismatchError(Currency, other.Currency);
        }

        return new Money(Amount - other.Amount, Currency);
    }

    /// <summary>
    /// Multiplies Money by a factor.
    /// </summary>
    /// <param name="factor">The factor to multiply by.</param>
    /// <returns>New Money with the multiplied amount.</returns>
    public Money Multiply(decimal factor)
    {
        return new Money(Amount * factor, Currency);
    }

    /// <summary>
    /// Divides Money by a divisor.
    /// </summary>
    /// <param name="divisor">The divisor.</param>
    /// <returns>Success with the divided Money; Failure with DivisionByZeroError if divisor is zero.</returns>
    public Result<Money, Error> Divide(decimal divisor)
    {
        if (divisor == 0)
        {
            return new DivisionByZeroError();
        }

        return new Money(Amount / divisor, Currency);
    }

    /// <summary>
    /// Allocates Money into equal parts using the Penny Allocation pattern.
    /// Distributes any remainder cents to the first parts to ensure the sum equals the original amount.
    /// </summary>
    /// <param name="parts">The number of parts to allocate into. Must be greater than zero.</param>
    /// <returns>Success with list of Money parts; Failure with InvalidAllocationError if parts is invalid.</returns>
    public Result<IReadOnlyList<Money>, Error> Allocate(int parts)
    {
        if (parts <= 0)
        {
            return new InvalidAllocationError("Number of parts must be greater than zero");
        }

        // Convert to cents to avoid decimal precision issues
        long amountInCents = (long)(Amount * 100);
        long baseAllocation = amountInCents / parts;
        long remainder = amountInCents % parts;

        List<Money> result = new(parts);

        for (int i = 0; i < parts; i++)
        {
            // Distribute remainder cents to first parts
            long allocation = baseAllocation + (i < remainder ? 1 : 0);
            result.Add(new Money(allocation / 100m, Currency));
        }

        return result;
    }

    /// <summary>
    /// Allocates Money according to specified ratios using the Penny Allocation pattern.
    /// </summary>
    /// <param name="ratios">The ratios for allocation. All must be non-negative, at least one must be positive.</param>
    /// <returns>Success with list of Money parts; Failure with InvalidAllocationError if ratios are invalid.</returns>
    public Result<IReadOnlyList<Money>, Error> Allocate(params int[] ratios)
    {
        if (ratios == null || ratios.Length == 0)
        {
            return new InvalidAllocationError("At least one ratio must be provided");
        }

        if (ratios.Any(r => r < 0))
        {
            return new InvalidAllocationError("Ratios cannot be negative");
        }

        int totalRatio = ratios.Sum();
        if (totalRatio == 0)
        {
            return new InvalidAllocationError("At least one ratio must be greater than zero");
        }

        // Convert to cents
        long amountInCents = (long)(Amount * 100);
        List<Money> result = new(ratios.Length);
        long allocated = 0;

        for (int i = 0; i < ratios.Length; i++)
        {
            long share;

            if (i == ratios.Length - 1)
            {
                // Last allocation gets any remainder to ensure sum equals original
                share = amountInCents - allocated;
            }
            else
            {
                share = (amountInCents * ratios[i]) / totalRatio;
                allocated += share;
            }

            result.Add(new Money(share / 100m, Currency));
        }

        return result;
    }

    /// <summary>
    /// Compares this Money instance to another.
    /// Throws ArgumentException if currencies don't match (required for operator overloads).
    /// </summary>
    /// <param name="other">The Money to compare to.</param>
    /// <returns>
    /// A value less than 0 if this is less than other;
    /// 0 if this equals other;
    /// A value greater than 0 if this is greater than other.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when currencies don't match.</exception>
    public int CompareTo(Money other)
    {
        if (!Currency.Equals(other.Currency, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Cannot compare Money with different currencies: '{Currency}' vs '{other.Currency}'",
                nameof(other));
        }

        return Amount.CompareTo(other.Amount);
    }

    public static bool operator <(Money left, Money right) => left.CompareTo(right) < 0;
    public static bool operator <=(Money left, Money right) => left.CompareTo(right) <= 0;
    public static bool operator >(Money left, Money right) => left.CompareTo(right) > 0;
    public static bool operator >=(Money left, Money right) => left.CompareTo(right) >= 0;
}
