using Menlo.Lib.Budget.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Menlo.Application.Common.ValueConverters;

internal sealed class CategoryIdValueConverter()
    : ValueConverter<CategoryId, Guid>(id => id.Value, guid => new CategoryId(guid));
