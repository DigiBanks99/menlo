namespace Menlo.Lib.Common.Errors;

/// <summary>
/// Error raised when attempting to divide money by zero.
/// </summary>
public sealed class DivisionByZeroError()
    : MoneyError("MONEY_002", "Cannot divide money by zero");
