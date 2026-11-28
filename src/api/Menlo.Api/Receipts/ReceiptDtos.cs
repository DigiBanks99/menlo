using Menlo.Lib.Common.ValueObjects;
using Menlo.Lib.Receipts.Entities;
using Menlo.Lib.Receipts.ValueObjects;

namespace Menlo.Api.Receipts;

/// <summary>
/// A receipt in the list: who took it, when, and where it is in being read.
/// </summary>
/// <param name="Id">The receipt's id.</param>
/// <param name="Year">The budget year it belongs to.</param>
/// <param name="Status">Waiting, Reading, ReadyToCheck or CouldntRead.</param>
/// <param name="TakenBy">The member who took the photos.</param>
/// <param name="TakenAt">When they were sent.</param>
/// <param name="Photos">How many photos it has; fetch each from <c>photos/{n}</c>, from 1.</param>
/// <param name="BudgetView">The budget view it was sent from, to go back to on Close.</param>
public sealed record ReceiptSummaryDto(
    Guid Id,
    int Year,
    string Status,
    Guid TakenBy,
    DateTimeOffset TakenAt,
    int Photos,
    string? BudgetView);

/// <summary>
/// One receipt, with what the agent read from it.
/// </summary>
/// <param name="Id">The receipt's id.</param>
/// <param name="Year">The budget year it belongs to.</param>
/// <param name="Status">Waiting, Reading, ReadyToCheck or CouldntRead.</param>
/// <param name="TakenBy">The member who took the photos.</param>
/// <param name="TakenAt">When they were sent.</param>
/// <param name="Photos">How many photos it has; fetch each from <c>photos/{n}</c>, from 1.</param>
/// <param name="BudgetView">The budget view it was sent from, to go back to on Close.</param>
/// <param name="Attempts">How many reads ran out of time.</param>
/// <param name="Reading">What the agent read, or null until it has been read.</param>
public sealed record ReceiptDto(
    Guid Id,
    int Year,
    string Status,
    Guid TakenBy,
    DateTimeOffset TakenAt,
    int Photos,
    string? BudgetView,
    int Attempts,
    ReceiptReadingDto? Reading)
{
    internal static ReceiptDto From(Receipt receipt, int photos) => new(
        receipt.Id.Value,
        receipt.Year,
        receipt.Status.ToString(),
        receipt.TakenBy.Value,
        receipt.TakenAt,
        photos,
        receipt.BudgetView,
        receipt.Attempts,
        ReceiptReadingDto.From(receipt.Reading));
}

/// <summary>
/// What the agent read from a receipt. Every field may be empty.
/// </summary>
public sealed record ReceiptReadingDto(
    ReceiptMoneyDto? Total,
    DateOnly? Date,
    string? Merchant,
    bool? IsRefund,
    IReadOnlyList<ReceiptLineDto> Lines,
    int? UnreadLines,
    bool? SawMoreThanOneTotal)
{
    internal static ReceiptReadingDto? From(ReceiptReading? reading) => reading is null
        ? null
        : new(
            ReceiptMoneyDto.From(reading.Total),
            reading.Date,
            reading.Merchant,
            reading.IsRefund,
            [.. reading.Lines.Select(line => new ReceiptLineDto(line.Description, ReceiptMoneyDto.From(line.Amount)))],
            reading.UnreadLines,
            reading.SawMoreThanOneTotal);
}

/// <summary>
/// One line item read from a receipt. Either part may be empty.
/// </summary>
public sealed record ReceiptLineDto(string? Description, ReceiptMoneyDto? Amount);

/// <summary>
/// An amount read from a receipt.
/// </summary>
public sealed record ReceiptMoneyDto(decimal Amount, string Currency)
{
    internal static ReceiptMoneyDto? From(Money? money) =>
        money is { } value ? new ReceiptMoneyDto(value.Amount, value.Currency) : null;
}
