namespace Menlo.Lib.Common.Errors;

/// <summary>
/// Error raised when allocation parameters are invalid.
/// </summary>
/// <param name="reason">The reason for the invalid allocation.</param>
public sealed class InvalidAllocationError(string reason)
    : MoneyError("MONEY_003", $"Invalid allocation: {reason}")
{
    /// <summary>
    /// Gets the reason why the allocation is invalid.
    /// </summary>
    public string Reason { get; } = reason;
}
