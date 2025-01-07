using System.Globalization;

namespace Menlo.Common;

public abstract record Measurement(decimal Value, IUnit Unit)
{
    public override string ToString()
    {
        return ToString(CultureInfo.InvariantCulture);
    }

    public virtual string ToString(IFormatProvider formatProvider)
    {
        return string.Create(formatProvider, $"{Value}{Unit.Symbol}");
    }
}
