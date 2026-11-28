using Menlo.Lib.Budget.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Menlo.Application.Common.ValueConverters;

internal sealed class RentalUnitIdValueConverter()
    : ValueConverter<RentalUnitId, Guid>(id => id.Value, guid => new RentalUnitId(guid));
