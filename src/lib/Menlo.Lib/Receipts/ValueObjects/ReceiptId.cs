namespace Menlo.Lib.Receipts.ValueObjects;

/// <summary>
/// Represents a strongly-typed identifier for a receipt.
/// </summary>
/// <param name="Value">The underlying unique identifier.</param>
public readonly record struct ReceiptId(Guid Value)
{
    /// <summary>
    /// Creates a new ReceiptId with a new unique value.
    /// </summary>
    public static ReceiptId NewId() => new(Guid.NewGuid());

    /// <summary>
    /// Returns the string representation of the receipt ID.
    /// </summary>
    public override string ToString() => Value.ToString();
}
