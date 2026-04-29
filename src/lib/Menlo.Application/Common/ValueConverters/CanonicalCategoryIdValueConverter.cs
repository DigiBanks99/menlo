using Menlo.Lib.Budget.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Menlo.Application.Common.ValueConverters;

internal sealed class CanonicalCategoryIdValueConverter()
    : ValueConverter<CanonicalCategoryId, Guid>(id => id.Value, guid => new CanonicalCategoryId(guid));
