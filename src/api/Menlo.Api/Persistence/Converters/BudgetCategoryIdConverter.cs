using Menlo.Lib.Budget.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Menlo.Api.Persistence.Converters;

/// <summary>
/// EF Core value converter for BudgetCategoryId strongly-typed ID.
/// Converts between BudgetCategoryId and Guid for database storage.
/// </summary>
public sealed class BudgetCategoryIdConverter : ValueConverter<BudgetCategoryId, Guid>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BudgetCategoryIdConverter"/> class.
    /// </summary>
    public BudgetCategoryIdConverter() : base(
        id => id.Value,
        guid => new BudgetCategoryId(guid))
    {
    }
}

/// <summary>
/// EF Core value converter for nullable BudgetCategoryId.
/// </summary>
public sealed class NullableBudgetCategoryIdConverter : ValueConverter<BudgetCategoryId?, Guid?>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NullableBudgetCategoryIdConverter"/> class.
    /// </summary>
    public NullableBudgetCategoryIdConverter() : base(
        id => id.HasValue ? id.Value.Value : null,
        guid => guid.HasValue ? new BudgetCategoryId(guid.Value) : null)
    {
    }
}
