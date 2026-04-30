using Menlo.Lib.Budget.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Menlo.Application.Common.ValueConverters;

internal sealed class BudgetItemIdValueConverter()
    : ValueConverter<BudgetItemId, Guid>(id => id.Value, guid => new BudgetItemId(guid));
