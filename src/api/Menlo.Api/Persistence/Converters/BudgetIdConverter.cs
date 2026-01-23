using Menlo.Lib.Budget.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Menlo.Api.Persistence.Converters;

/// <summary>
/// EF Core value converter for BudgetId strongly-typed ID.
/// Converts between BudgetId and Guid for database storage.
/// </summary>
public sealed class BudgetIdConverter : ValueConverter<BudgetId, Guid>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BudgetIdConverter"/> class.
    /// </summary>
    public BudgetIdConverter() : base(
        id => id.Value,
        guid => new BudgetId(guid))
    {
    }
}

/// <summary>
/// EF Core value converter for nullable BudgetId.
/// </summary>
public sealed class NullableBudgetIdConverter : ValueConverter<BudgetId?, Guid?>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NullableBudgetIdConverter"/> class.
    /// </summary>
    public NullableBudgetIdConverter() : base(
        id => id.HasValue ? id.Value.Value : null,
        guid => guid.HasValue ? new BudgetId(guid.Value) : null)
    {
    }
}
