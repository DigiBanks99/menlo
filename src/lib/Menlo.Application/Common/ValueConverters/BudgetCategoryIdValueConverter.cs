using Menlo.Lib.Budget.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Menlo.Application.Common.ValueConverters;

internal sealed class BudgetCategoryIdValueConverter()
    : ValueConverter<BudgetCategoryId, Guid>(id => id.Value, guid => new BudgetCategoryId(guid));
