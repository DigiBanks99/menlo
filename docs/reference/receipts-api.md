# Receipts API Reference

A receipt is 1 to 3 photos of a payment, such as a till slip, that the agent reads for a member to check (see the glossary). Receipts belong to a budget year, and the endpoints live under `/api/budgets/{year}/receipts`. The code is in `src/api/Menlo.Api/Receipts/`; the aggregate is `Menlo.Lib.Receipts.Entities.Receipt`.

## Common behaviour

- **Sign-in.** Every endpoint needs a signed-in user. Every member can do everything: there is no receipt policy.
- **Feature flag.** Every endpoint returns `404` while `Features:Budget` is off.
- **Anti-forgery.** `POST` and `DELETE` need the `X-XSRF-TOKEN` header (see [Anti-forgery](authentication-api.md#anti-forgery)).
- **Year.** A receipt is only found under its own year: asking for it under another year returns `404`.
- **Handlers.** The endpoints call `ReceiptHandlers`, which pages call too.

## Endpoints

| Endpoint                                         | Does                                                                                                     |
| ------------------------------------------------ | -------------------------------------------------------------------------------------------------------- |
| `POST /api/budgets/{year}/receipts`              | Sends a receipt. Returns `202` with the receipt, Waiting, and its URL in `Location`.                     |
| `GET /api/budgets/{year}/receipts`               | Lists the year's receipts, newest first: `id`, `year`, `status`, `takenBy`, `takenAt`, `photos`, `budgetView`. |
| `GET /api/budgets/{year}/receipts/{id}`          | Gets one receipt: the list's fields, plus `attempts` and `reading`, what was read (`null` until read).  |
| `GET /api/budgets/{year}/receipts/{id}/photos/{n}` | Gets photo `n`, counting from 1, as `image/jpeg`. `404` if there is no such photo.                     |
| `DELETE /api/budgets/{year}/receipts/{id}`       | Discards the receipt, photos and all. Returns `204`.                                                     |

`status` is `Waiting`, `Reading`, `ReadyToCheck` or `CouldntRead`. `reading` has `total`, `date`, `merchant`, `isRefund`, `lines` (each a `description` and an `amount`), `unreadLines` and `sawMoreThanOneTotal`; any of them may be empty. Amounts are `{ amount, currency }`.

## Sending a receipt

The request is `multipart/form-data`:

- one file field per photo, under any name (the page uses `photos`), in order;
- an optional `budgetView` field: the path of the budget view it was sent from, kept so closing the receipt can go back there. Anything that isn't a path within Menlo is dropped: it must start with a single `/` and hold no control characters, since a browser drops tabs and line breaks from a URL and `/<tab>/example.com` would become `//example.com`.

The body is read straight from the request with a `MultipartReader`, so photos never go to a temporary file. A request may be at most 64 MB. A file field left blank (empty file name) is ignored.

It returns `400` with a problem-details body, and keeps nothing, when:

- there is no photo, or more than 3 (`code` `Receipt.InvalidPhotoCount`);
- a file isn't an image Menlo takes (`Receipt.UnreadablePhoto`, naming the file);
- the request isn't `multipart/form-data`.

### What happens to each photo

1. **Sniffed.** Its first bytes must be a JPEG, PNG, WebP or HEIC (an ISO `ftyp` box with a HEIF brand such as `heic` or `mif1`). The file name and content type are never trusted.
2. **Decoded** by Magick.NET, only as the sniffed format. A photo over 100 megapixels is refused before decoding, and ImageMagick's own limits, set at startup, refuse any photo wider or taller than 16,384 pixels.
3. **Turned upright** from its EXIF orientation, because the model server ignores it.
4. **Scaled down** to at most 4 megapixels, keeping its shape. Smaller photos keep their size.
5. **Stripped** of its metadata, location included, and **stored as a JPEG** (quality 85).

## Statuses

`Receipt` enforces these moves; the reading worker that makes them is still to come ([Task: The reading worker and the model gate](https://github.com/DigiBanks99/menlo/issues/786)), so for now every receipt stays Waiting. A receipt starts Waiting. Reading it moves it to Reading, then to ReadyToCheck when it read anything usable (a total, date, merchant or line) or CouldntRead when it didn't. If the model is down, or Menlo restarts mid-read, it goes back to Waiting and the read doesn't count. A read that runs out of time counts as an attempt: the first goes back to Waiting, the second ends in CouldntRead.

## Storage

Receipts are in `financial.receipts`, with what was read as JSONB, and photos are `bytea` rows in `financial.receipt_photos`, keyed by receipt and position. Receipts are deleted, not soft-deleted, so their photos go with them (see [Persistence](persistence.md)).
