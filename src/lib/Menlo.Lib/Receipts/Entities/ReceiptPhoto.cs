using Menlo.Lib.Receipts.ValueObjects;

namespace Menlo.Lib.Receipts.Entities;

/// <summary>
/// One photo of a receipt, stored as a JPEG. Photos are fixed once sent and go with their receipt.
/// </summary>
public sealed class ReceiptPhoto
{
    internal ReceiptPhoto(ReceiptId receiptId, int position, byte[] jpeg)
    {
        ReceiptId = receiptId;
        Position = position;
        Jpeg = jpeg;
    }

    // Required by EF Core for materialization
    private ReceiptPhoto()
    {
        Jpeg = [];
    }

    /// <summary>Gets the receipt the photo belongs to.</summary>
    public ReceiptId ReceiptId { get; }

    /// <summary>Gets where the photo comes in the receipt, from 1: the pages of one slip, in the order sent.</summary>
    public int Position { get; }

    /// <summary>Gets the photo, as JPEG bytes.</summary>
    public byte[] Jpeg { get; }
}
