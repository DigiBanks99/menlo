using Menlo.Lib.Receipts.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Menlo.Application.Common.ValueConverters;

internal sealed class ReceiptIdValueConverter()
    : ValueConverter<ReceiptId, Guid>(id => id.Value, guid => new ReceiptId(guid));
