using Menlo.Lib.Common.ValueObjects;

namespace Menlo.Lib.Receipts.ValueObjects;

/// <summary>
/// What the agent read from a receipt's photos, kept for a member to check. Every field may be empty: the agent
/// leaves out what it couldn't read rather than guess.
/// </summary>
/// <param name="Total">The total paid, or refunded.</param>
/// <param name="Date">The date on the slip.</param>
/// <param name="Merchant">Who was paid.</param>
/// <param name="IsRefund">Whether the slip is a refund.</param>
/// <param name="Lines">The line items it could read, in the slip's order.</param>
/// <param name="UnreadLines">The number of lines it saw but couldn't read.</param>
/// <param name="SawMoreThanOneTotal">Whether it saw more than one total on the slip.</param>
public sealed record ReceiptReading(
    Money? Total,
    DateOnly? Date,
    string? Merchant,
    bool? IsRefund,
    IReadOnlyList<ReceiptLine> Lines,
    int? UnreadLines,
    bool? SawMoreThanOneTotal)
{
    /// <summary>
    /// Gets a reading with nothing in it.
    /// </summary>
    public static ReceiptReading Empty { get; } = new(null, null, null, null, [], null, null);

    /// <summary>
    /// Gets whether the reading holds anything a member could check: a total, a date, a merchant or a line.
    /// </summary>
    public bool HasAnythingUsable =>
        Total is not null
        || Date is not null
        || !string.IsNullOrWhiteSpace(Merchant)
        || Lines.Any(line => line.Amount is not null || !string.IsNullOrWhiteSpace(line.Description));
}

/// <summary>
/// One line item read from a receipt. Either part may be empty.
/// </summary>
/// <param name="Description">What the line is for.</param>
/// <param name="Amount">The line's amount.</param>
public sealed record ReceiptLine(string? Description, Money? Amount);
