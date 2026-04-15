using Menlo.Lib.Common.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Menlo.Application.Common.ValueConverters;

internal sealed class UserIdValueConverter()
    : ValueConverter<UserId, Guid>(id => id.Value, guid => new UserId(guid));


