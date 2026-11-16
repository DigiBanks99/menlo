using System.Globalization;

namespace Menlo.Ui.Utilities;

/// <summary>
/// Formats amounts the way Menlo shows them: a currency symbol, a space as the thousands separator and
/// two decimals, for example <c>R 12 000.00</c>.
/// </summary>
public static class MoneyFormat
{
    private static readonly NumberFormatInfo Numbers = new()
    {
        NumberGroupSeparator = " ",
        NumberDecimalSeparator = ".",
        NumberDecimalDigits = 2
    };

    /// <summary>
    /// Formats an amount with its currency symbol. Unknown currencies use the code as the symbol.
    /// </summary>
    public static string Format(decimal amount, string currency = "ZAR") =>
        $"{Symbol(currency)} {Number(amount)}";

    /// <summary>
    /// Formats an amount, or returns <paramref name="fallback"/> when there is none.
    /// </summary>
    public static string FormatOrDefault(decimal? amount, string currency = "ZAR", string fallback = "") =>
        amount is null ? fallback : Format(amount.Value, currency);

    /// <summary>
    /// Formats the number alone, with grouping and two decimals.
    /// </summary>
    public static string Number(decimal amount) => amount.ToString("N2", Numbers);

    /// <summary>
    /// Formats a value for an input: no grouping, a dot as the decimal separator, no trailing zeros.
    /// </summary>
    public static string Editable(decimal? amount) =>
        amount?.ToString("0.##", CultureInfo.InvariantCulture) ?? string.Empty;

    /// <summary>
    /// Parses what a person typed into an amount field. Spaces and thousands separators are ignored and a
    /// comma is accepted as the decimal separator. Returns false for anything that isn't a number.
    /// </summary>
    public static bool TryParse(string? text, out decimal amount)
    {
        amount = 0m;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        string cleaned = text
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("R", string.Empty, StringComparison.OrdinalIgnoreCase);

        // A single comma with no dot is a decimal comma; otherwise commas are thousands separators
        cleaned = cleaned.Contains('.', StringComparison.Ordinal) || cleaned.Count(c => c == ',') != 1
            ? cleaned.Replace(",", string.Empty, StringComparison.Ordinal)
            : cleaned.Replace(',', '.');

        return decimal.TryParse(
            cleaned,
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture,
            out amount);
    }

    /// <summary>
    /// Returns the symbol shown for a currency code.
    /// </summary>
    public static string Symbol(string currency) => currency.ToUpperInvariant() switch
    {
        "ZAR" => "R",
        "USD" => "$",
        "EUR" => "€",
        "GBP" => "£",
        _ => currency.ToUpperInvariant()
    };
}
