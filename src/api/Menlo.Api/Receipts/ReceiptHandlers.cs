using CSharpFunctionalExtensions;
using Menlo.Api.Receipts.Photos;
using Menlo.Application.Auth;
using Menlo.Application.Receipts;
using Menlo.Lib.Common;
using Menlo.Lib.Receipts.Entities;
using Menlo.Lib.Receipts.Errors;
using Menlo.Lib.Receipts.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Menlo.Api.Receipts;

/// <summary>
/// What can be done with a budget year's receipts: send one, list them, get one and its photos, and discard one. The
/// JSON endpoints and the pages both call these.
/// </summary>
public sealed class ReceiptHandlers(IReceiptContext receipts, CurrentUser currentUser, TimeProvider timeProvider)
{
    /// <summary>
    /// Sends a receipt of 1 to 3 photos for the year. Each photo is checked to be an image and kept as a JPEG, and the
    /// receipt waits to be read.
    /// </summary>
    public async Task<Result<ReceiptDto, ReceiptError>> SendAsync(int year, ReceiptUpload upload, CancellationToken cancellationToken)
    {
        if (upload.PhotosSent is 0 or > Receipt.MaxPhotos)
        {
            return new InvalidPhotoCountError(upload.PhotosSent);
        }

        List<byte[]> jpegs = [];
        for (int index = 0; index < upload.Photos.Count; index++)
        {
            if (ReceiptPhotoConverter.ToJpeg(upload.Photos[index]) is not { } jpeg)
            {
                return new UnreadablePhotoError(index + 1);
            }

            jpegs.Add(jpeg);
        }

        Result<Receipt, ReceiptError> sent = Receipt.Send(
            year,
            currentUser.Value?.UserId ?? SystemUser.Id,
            timeProvider.GetUtcNow(),
            jpegs,
            upload.BudgetView);
        if (sent.IsFailure)
        {
            return sent.Error;
        }

        receipts.Receipts.Add(sent.Value);
        await receipts.SaveChangesAsync(cancellationToken);

        return ReceiptDto.From(sent.Value, jpegs.Count);
    }

    /// <summary>
    /// Lists the year's receipts that are not yet saved or discarded, newest first.
    /// </summary>
    public async Task<IReadOnlyList<ReceiptSummaryDto>> ListAsync(int year, CancellationToken cancellationToken)
    {
        var rows = await receipts.Receipts
            .AsNoTracking()
            .Where(r => r.Year == year)
            .OrderByDescending(r => r.TakenAt)
            .Select(r => new { r.Id, r.Status, r.TakenBy, r.TakenAt, Photos = r.Photos.Count, r.BudgetView })
            .ToListAsync(cancellationToken);

        return
        [
            .. rows.Select(r => new ReceiptSummaryDto(
                r.Id.Value, year, r.Status.ToString(), r.TakenBy.Value, r.TakenAt, r.Photos, r.BudgetView))
        ];
    }

    /// <summary>
    /// Gets one of the year's receipts with what was read, or null when it isn't one of them.
    /// </summary>
    public async Task<ReceiptDto?> GetAsync(int year, Guid id, CancellationToken cancellationToken)
    {
        ReceiptId receiptId = new(id);
        Receipt? receipt = await receipts.Receipts
            .AsNoTracking()
            .SingleOrDefaultAsync(r => r.Id == receiptId && r.Year == year, cancellationToken);
        if (receipt is null)
        {
            return null;
        }

        int photos = await receipts.ReceiptPhotos.CountAsync(p => p.ReceiptId == receiptId, cancellationToken);
        return ReceiptDto.From(receipt, photos);
    }

    /// <summary>
    /// Gets photo <paramref name="position"/> (from 1) of one of the year's receipts, as JPEG bytes, or null when there
    /// is no such photo.
    /// </summary>
    public Task<byte[]?> GetPhotoAsync(int year, Guid id, int position, CancellationToken cancellationToken)
    {
        ReceiptId receiptId = new(id);
        return receipts.ReceiptPhotos
            .AsNoTracking()
            .Where(p => p.ReceiptId == receiptId
                && p.Position == position
                && receipts.Receipts.Any(r => r.Id == receiptId && r.Year == year))
            .Select(p => p.Jpeg)
            .SingleOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Discards one of the year's receipts, photos and all. Returns false when it isn't one of them.
    /// </summary>
    public async Task<bool> DiscardAsync(int year, Guid id, CancellationToken cancellationToken)
    {
        ReceiptId receiptId = new(id);
        Receipt? receipt = await receipts.Receipts
            .SingleOrDefaultAsync(r => r.Id == receiptId && r.Year == year, cancellationToken);
        if (receipt is null)
        {
            return false;
        }

        receipts.Receipts.Remove(receipt);
        await receipts.SaveChangesAsync(cancellationToken);
        return true;
    }
}
