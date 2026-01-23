using Menlo.Lib.Common.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Menlo.Api.Persistence.Converters;

/// <summary>
/// EF Core value converter for nullable Money value object.
/// Stores Money as a JSON string in the database.
/// </summary>
public sealed class NullableMoneyConverter : ValueConverter<Money?, string?>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NullableMoneyConverter"/> class.
    /// </summary>
    public NullableMoneyConverter() : base(
        money => money.HasValue ? $"{money.Value.Amount}|{money.Value.Currency}" : null,
        value => value != null ? ParseMoney(value) : null)
    {
    }

    private static Money? ParseMoney(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        string[] parts = value.Split('|');
        if (parts.Length != 2)
            return null;

        if (!decimal.TryParse(parts[0], out decimal amount))
            return null;

        var result = Money.Create(amount, parts[1]);
        return result.IsSuccess ? result.Value : null;
    }
}