using CSharpFunctionalExtensions;
using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Common.Enums;
using Menlo.Lib.Common.ValueObjects;
using Menlo.Lib.Receipts.Enums;
using Menlo.Lib.Receipts.Errors;
using Menlo.Lib.Receipts.ValueObjects;

namespace Menlo.Lib.Receipts.Entities;

/// <summary>
/// Aggregate root for a receipt: 1 to 3 photos of a payment that the agent reads for a member to check. It belongs
/// to a budget year, and is deleted, photos and all, once it's saved as a payment or discarded.
/// </summary>
public sealed class Receipt : IAggregateRoot<ReceiptId>, IAuditable
{
    /// <summary>The most photos one receipt holds.</summary>
    public const int MaxPhotos = 3;

    /// <summary>The reads that may run out of time: the first is retried, the second ends in Couldn't read.</summary>
    public const int MaxTimeouts = 2;

    private readonly List<ReceiptPhoto> _photos = [];

    private Receipt(ReceiptId id, int year, UserId takenBy, DateTimeOffset takenAt, string? budgetView)
    {
        Id = id;
        Year = year;
        TakenBy = takenBy;
        TakenAt = takenAt;
        BudgetView = budgetView;
        Status = ReceiptStatus.Waiting;
    }

    /// <summary>Gets the unique identifier for this receipt.</summary>
    public ReceiptId Id { get; }

    /// <summary>Gets the budget year the receipt belongs to.</summary>
    public int Year { get; }

    /// <summary>Gets the member who took the photos.</summary>
    public UserId TakenBy { get; }

    /// <summary>Gets when the photos were sent.</summary>
    public DateTimeOffset TakenAt { get; }

    /// <summary>Gets the budget view the receipt was sent from, so closing it can go back there.</summary>
    public string? BudgetView { get; }

    /// <summary>Gets where the receipt is in being read.</summary>
    public ReceiptStatus Status { get; private set; }

    /// <summary>Gets how many reads of this receipt have run out of time.</summary>
    public int Attempts { get; private set; }

    /// <summary>Gets what the agent read, or null until it has been read.</summary>
    public ReceiptReading? Reading { get; private set; }

    /// <summary>Gets the receipt's photos, in the order sent. Not loaded unless asked for.</summary>
    public IReadOnlyList<ReceiptPhoto> Photos => _photos.AsReadOnly();

    // IAuditable
    /// <inheritdoc />
    public UserId? CreatedBy { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? CreatedAt { get; private set; }

    /// <inheritdoc />
    public UserId? ModifiedBy { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedAt { get; private set; }

    /// <inheritdoc />
    public void Audit(IAuditStampFactory factory, AuditOperation operation)
    {
        AuditStamp stamp = factory.CreateStamp();
        if (operation == AuditOperation.Create)
        {
            CreatedBy = stamp.ActorId;
            CreatedAt = stamp.Timestamp;
        }

        ModifiedBy = stamp.ActorId;
        ModifiedAt = stamp.Timestamp;
    }

    /// <summary>
    /// Sends a receipt of 1 to 3 photos to be read. It starts out waiting.
    /// </summary>
    /// <param name="year">The budget year it belongs to.</param>
    /// <param name="takenBy">The member who took the photos.</param>
    /// <param name="takenAt">When they were sent.</param>
    /// <param name="photos">The photos, as JPEG bytes, in order.</param>
    /// <param name="budgetView">The budget view it was sent from, if any.</param>
    /// <returns>The new receipt, or why it can't be sent.</returns>
    public static Result<Receipt, ReceiptError> Send(
        int year,
        UserId takenBy,
        DateTimeOffset takenAt,
        IReadOnlyList<byte[]> photos,
        string? budgetView)
    {
        if (year is < 1 or > 9999)
        {
            return new InvalidReceiptYearError(year);
        }

        if (photos.Count is 0 or > MaxPhotos)
        {
            return new InvalidPhotoCountError(photos.Count);
        }

        Receipt receipt = new(ReceiptId.NewId(), year, takenBy, takenAt, string.IsNullOrWhiteSpace(budgetView) ? null : budgetView);
        for (int index = 0; index < photos.Count; index++)
        {
            receipt._photos.Add(new ReceiptPhoto(receipt.Id, index + 1, photos[index]));
        }

        return receipt;
    }

    /// <summary>
    /// The agent starts reading a waiting receipt.
    /// </summary>
    public UnitResult<ReceiptError> StartReading()
    {
        if (Status != ReceiptStatus.Waiting)
        {
            return new InvalidReceiptStatusError("start being read", Status);
        }

        Status = ReceiptStatus.Reading;
        return UnitResult.Success<ReceiptError>();
    }

    /// <summary>
    /// The agent finished reading. With anything usable the receipt is ready to check; otherwise it couldn't be read.
    /// </summary>
    /// <param name="reading">What the agent read.</param>
    public UnitResult<ReceiptError> Read(ReceiptReading reading)
    {
        if (Status != ReceiptStatus.Reading)
        {
            return new InvalidReceiptStatusError("be read", Status);
        }

        Reading = reading;
        Status = reading.HasAnythingUsable ? ReceiptStatus.ReadyToCheck : ReceiptStatus.CouldntRead;
        return UnitResult.Success<ReceiptError>();
    }

    /// <summary>
    /// The read stopped before it finished, because the model is down or Menlo restarted. The receipt waits to be
    /// read again, and the read doesn't count as an attempt.
    /// </summary>
    public UnitResult<ReceiptError> ReturnToWaiting()
    {
        if (Status != ReceiptStatus.Reading)
        {
            return new InvalidReceiptStatusError("go back to waiting", Status);
        }

        Status = ReceiptStatus.Waiting;
        return UnitResult.Success<ReceiptError>();
    }

    /// <summary>
    /// The read ran out of time. The first time the receipt waits to be read again; the second time it couldn't be read.
    /// </summary>
    public UnitResult<ReceiptError> TimeOut()
    {
        if (Status != ReceiptStatus.Reading)
        {
            return new InvalidReceiptStatusError("run out of time", Status);
        }

        Attempts++;
        Status = Attempts >= MaxTimeouts ? ReceiptStatus.CouldntRead : ReceiptStatus.Waiting;
        return UnitResult.Success<ReceiptError>();
    }
}
