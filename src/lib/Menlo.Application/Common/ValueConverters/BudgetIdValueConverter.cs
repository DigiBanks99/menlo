using Menlo.Lib.Budget.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Menlo.Application.Common.ValueConverters;

internal sealed class BudgetIdValueConverter()
    : ValueConverter<BudgetId, Guid>(id => id.Value, guid => new BudgetId(guid));
