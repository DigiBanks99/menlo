using JetBrains.Annotations;
using Menlo.Common.Extensions;
using System.Globalization;

namespace Menlo.Common;

[TestSubject(typeof(Measurement))]
public class MeasurementTests
{
    [Theory]
    [InlineData("en-US", "1234.56mu")]
    [InlineData("en-ZA", "1234,56mu")]
    public void ToString_ShouldReturnAFormattedString(string culture, string expected)
    {
        MyMeasurement measurement = new(1234.56m);

        measurement.ToString(CultureInfoExtensions.GetCultureInfo(culture)).ShouldBe(expected);
    }

    record MyUnit : IUnit
    {
        public string Name { get; } = "MyUnit";
        public string Symbol { get; } = "mu";
    }

    record MyMeasurement(decimal Value) : Measurement(Value, new MyUnit());
}
