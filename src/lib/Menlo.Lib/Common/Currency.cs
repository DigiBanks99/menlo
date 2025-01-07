namespace Menlo.Common;

public record Currency(string Code, string Symbol, string Name)
{
    public static Currency Zar => new("ZAR", "R", "South African Rand");
    public static Currency Usd => new("USD", "$", "United States Dollar");
    public static Currency Eur => new("EUR", "€", "Euro");
    public static Currency Gbp => new("GBP", "£", "British Pound Sterling");
    public static Currency Aud => new("AUD", "$", "Australian Dollar");
    public static Currency Cad => new("CAD", "$", "Canadian Dollar");
    public static Currency Jpy => new("JPY", "¥", "Japanese Yen");
    public static Currency Cny => new("CNY", "¥", "Chinese Yuan");
    public static Currency Inr => new("INR", "₹", "Indian Rupee");

    public static Currency FromCode(string code)
    {
        return code switch
        {
            "ZAR" => Zar,
            "USD" => Usd,
            "EUR" => Eur,
            "GBP" => Gbp,
            "AUD" => Aud,
            "CAD" => Cad,
            "JPY" => Jpy,
            "CNY" => Cny,
            "INR" => Inr,
            _ => throw new ArgumentException($"Currency code {code} is not supported")
        };
    }
}
