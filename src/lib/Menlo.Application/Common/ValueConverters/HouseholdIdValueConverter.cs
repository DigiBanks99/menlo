using Menlo.Lib.Common.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Menlo.Application.Common.ValueConverters;

internal sealed class HouseholdIdValueConverter()
    : ValueConverter<HouseholdId, Guid>(id => id.Value, guid => new HouseholdId(guid));
