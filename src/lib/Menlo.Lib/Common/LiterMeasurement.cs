using Menlo.Common.Converters;
using Newtonsoft.Json;
using System.Globalization;

namespace Menlo.Common;

[JsonConverter(typeof(LiterMeasurementJsonNetConverter))]
public record LiterMeasurement(decimal Value) : Measurement(Value, Liter.Instance), IComparable<LiterMeasurement>
{
    public static LiterMeasurement Zero => new(decimal.Zero);

    public override string ToString()
    {
        return ToString(CultureInfo.InvariantCulture);
    }

    public override string ToString(IFormatProvider formatProvider)
    {
        (decimal value, string unit) = Value switch
        {
            <= 1000m => (Value, $"{Unit}"),
            > 1000m and <= 1000_000m => (Value / 1000m, $"k{Unit}"),
            > 1000_000m => (Value / 1000_000m, $"m{Unit}")
        };

        return string.Create(formatProvider, $"{value}{unit}");
    }

    public int CompareTo(LiterMeasurement? other)
    {
        if (other is null)
        {
            return 1;
        }

        if (other.Unit != Unit)
        {
            throw new InvalidOperationException("Cannot compare measurements with different units.");
        }

        return Value.CompareTo(other.Value);
    }

    public static LiterMeasurement operator -(LiterMeasurement left, LiterMeasurement right)
    {
        return new LiterMeasurement(left.Value - right.Value);
    }

    public static LiterMeasurement operator +(LiterMeasurement left, LiterMeasurement right)
    {
        return new LiterMeasurement(left.Value + right.Value);
    }

    public static LiterMeasurement operator *(LiterMeasurement left, decimal right)
    {
        return new LiterMeasurement(left.Value * right);
    }

    public static LiterMeasurement operator /(LiterMeasurement left, decimal right)
    {
        return new LiterMeasurement(left.Value / right);
    }
}
