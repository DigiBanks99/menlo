using Menlo.Lib.Receipts.Entities;
using Microsoft.EntityFrameworkCore;

namespace Menlo.Application.Receipts;

public interface IReceiptContext
{
    DbSet<Receipt> Receipts { get; }

    /// <summary>
    /// The receipts' photos, for reading one photo without loading its receipt. Add and remove them through the receipt.
    /// </summary>
    DbSet<ReceiptPhoto> ReceiptPhotos { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
