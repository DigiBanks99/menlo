using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Receipts.Entities;
using Menlo.Lib.Receipts.Enums;

namespace Menlo.Lib.Receipts.Errors;

/// <summary>
/// Base class for receipt domain errors.
/// </summary>
/// <param name="code">Machine-readable error code.</param>
/// <param name="message">Human-readable error message.</param>
public class ReceiptError(string code, string message) : Error(code, message);

/// <summary>
/// Error indicating a receipt was sent with no photos, or with more than it can hold.
/// </summary>
/// <param name="count">The number of photos sent.</param>
public sealed class InvalidPhotoCountError(int count)
    : ReceiptError(
        "Receipt.InvalidPhotoCount",
        count == 0
            ? "Send at least one photo of the slip."
            : $"A receipt holds at most {Receipt.MaxPhotos} photos, but {count} were sent.")
{
    /// <summary>Gets the number of photos sent.</summary>
    public int Count { get; } = count;
}

/// <summary>
/// Error indicating a receipt was sent for a year that can't be a budget year.
/// </summary>
/// <param name="year">The year given.</param>
public sealed class InvalidReceiptYearError(int year)
    : ReceiptError("Receipt.InvalidYear", $"{year} isn't a budget year.")
{
    /// <summary>Gets the year given.</summary>
    public int Year { get; } = year;
}

/// <summary>
/// Error indicating a receipt can't make a change from the status it's in.
/// </summary>
/// <param name="operation">The change attempted.</param>
/// <param name="status">The receipt's status.</param>
public sealed class InvalidReceiptStatusError(string operation, ReceiptStatus status)
    : ReceiptError("Receipt.InvalidStatus", $"A receipt that is {status} can't {operation}.")
{
    /// <summary>Gets the change attempted.</summary>
    public string Operation { get; } = operation;

    /// <summary>Gets the receipt's status.</summary>
    public ReceiptStatus Status { get; } = status;
}
