namespace Menlo.Lib.Common.Errors;

/// <summary>
/// Error raised when attempting to perform operations on Money objects with different currencies.
/// </summary>
/// <param name="expected">The expected currency code.</param>
/// <param name="actual">The actual currency code provided.</param>
public sealed class CurrencyMismatchError(string expected, string actual)
    : MoneyError("MONEY_001", $"Currency mismatch: expected '{expected}' but got '{actual}'")
{
    /// <summary>
    /// Gets the expected currency code.
    /// </summary>
    public string ExpectedCurrency { get; } = expected;

    /// <summary>
    /// Gets the actual currency code that was provided.
    /// </summary>
    public string ActualCurrency { get; } = actual;
}
