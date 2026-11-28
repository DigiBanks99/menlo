namespace Menlo.Lib.Receipts.Enums;

/// <summary>
/// Where a receipt is in being read. A receipt waits, is read, and is then ready to check or couldn't be read.
/// </summary>
public enum ReceiptStatus
{
    /// <summary>
    /// Sent, and waiting for the agent to read it. A receipt goes back to waiting when the model is down or its
    /// first read ran out of time.
    /// </summary>
    Waiting,

    /// <summary>
    /// The agent is reading it.
    /// </summary>
    Reading,

    /// <summary>
    /// Read, and waiting for a member to check it into a payment.
    /// </summary>
    ReadyToCheck,

    /// <summary>
    /// The agent found nothing usable, or ran out of time twice. A member can still check it in by hand.
    /// </summary>
    CouldntRead,
}
