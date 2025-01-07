using JetBrains.Annotations;
using System.Globalization;

namespace Menlo.Common;

[TestSubject(typeof(LiterMeasurement))]
public class LiterMeasurementTests
{
    [Theory]
    [InlineData(1.5, "1.5l")]
    [InlineData(1.599, "1.599l")]
    [InlineData(15, "15l")]
    [InlineData(159, "159l")]
    [InlineData(1000, "1000l")]
    [InlineData(1001, "1.001kl")]
    [InlineData(1_599, "1.599kl")]
    [InlineData(15_999, "15.999kl")]
    [InlineData(159_999, "159.999kl")]
    [InlineData(1000_000, "1000kl")]
    [InlineData(1000_001, "1.000001ml")]
    public void ToString_ShouldReturn_TheCorrectDescription(decimal value, string expected)
    {
        LiterMeasurement measurement = new(value);
        measurement.ToString().ShouldBe(expected);
    }

    [Theory]
    [InlineData(1.5, "1,5l", "en-ZA")]
    [InlineData(1.5, "1,5l", "af-ZA")]
    [InlineData(1.599, "1,599l", "en-ZA")]
    [InlineData(15, "15l", "en-ZA")]
    [InlineData(159, "159l", "en-ZA")]
    [InlineData(1000, "1000l", "en-ZA")]
    [InlineData(1001, "1,001kl", "en-ZA")]
    [InlineData(1_599, "1,599kl", "en-ZA")]
    [InlineData(15_999, "15,999kl", "en-ZA")]
    [InlineData(159_999, "159,999kl", "en-ZA")]
    [InlineData(1000_000, "1000kl", "en-ZA")]
    [InlineData(1000_001, "1,000001ml", "en-ZA")]
    [InlineData(1.5, "1,5l", "fr-FR")]
    [InlineData(1.599, "1,599l", "fr-FR")]
    [InlineData(15, "15l", "fr-FR")]
    [InlineData(159, "159l", "fr-FR")]
    [InlineData(1000, "1000l", "fr-FR")]
    [InlineData(1001, "1,001kl", "fr-FR")]
    [InlineData(1_599, "1,599kl", "fr-FR")]
    [InlineData(15_999, "15,999kl", "fr-FR")]
    [InlineData(159_999, "159,999kl", "fr-FR")]
    [InlineData(1000_000, "1000kl", "fr-FR")]
    [InlineData(1000_001, "1,000001ml", "fr-FR")]
    [InlineData(1.5, "1.5l", "en-GB")]
    [InlineData(1.599, "1.599l", "en-GB")]
    [InlineData(15, "15l", "en-GB")]
    [InlineData(159, "159l", "en-GB")]
    [InlineData(1000, "1000l", "en-GB")]
    [InlineData(1001, "1.001kl", "en-GB")]
    [InlineData(1_599, "1.599kl", "en-GB")]
    [InlineData(15_999, "15.999kl", "en-GB")]
    [InlineData(159_999, "159.999kl", "en-GB")]
    [InlineData(1000_000, "1000kl", "en-GB")]
    [InlineData(1000_001, "1.000001ml", "en-GB")]
    [InlineData(1.5, "1.5l", "en-IN")]
    [InlineData(1.599, "1.599l", "en-IN")]
    [InlineData(15, "15l", "en-IN")]
    [InlineData(159, "159l", "en-IN")]
    [InlineData(1000, "1000l", "en-IN")]
    [InlineData(1001, "1.001kl", "en-IN")]
    [InlineData(1_599, "1.599kl", "en-IN")]
    [InlineData(15_999, "15.999kl", "en-IN")]
    [InlineData(159_999, "159.999kl", "en-IN")]
    [InlineData(1000_000, "1000kl", "en-IN")]
    [InlineData(1000_001, "1.000001ml", "en-IN")]
    public void ToString_ShouldReturn_TheCorrectDescription_WithCultureInfo(decimal value, string expected,
        string cultureInfo)
    {
        LiterMeasurement measurement = new(value);
        measurement.ToString(CultureInfo.GetCultureInfo(cultureInfo)).ShouldBe(expected);
    }

    [Fact]
    public void Operator_Subtract_ShouldReturnCorrectResult()
    {
        LiterMeasurement left = new(2000m);
        LiterMeasurement right = new(1000m);
        LiterMeasurement result = left - right;
        result.Value.ShouldBe(1000m);
    }

    [Fact]
    public void Operator_Add_ShouldReturnCorrectResult()
    {
        LiterMeasurement left = new(1000m);
        LiterMeasurement right = new(2000m);
        LiterMeasurement result = left + right;
        result.Value.ShouldBe(3000m);
    }

    [Fact]
    public void Operator_Multiply_ShouldReturnCorrectResult()
    {
        LiterMeasurement left = new(1000m);
        LiterMeasurement result = left * 2m;
        result.Value.ShouldBe(2000m);
    }

    [Fact]
    public void Operator_Divide_ShouldReturnCorrectResult()
    {
        LiterMeasurement left = new(2000m);
        LiterMeasurement result = left / 2m;
        result.Value.ShouldBe(1000m);
    }

    [Fact]
    public void Operator_LessThan_ShouldReturnCorrectResult()
    {
        LiterMeasurement left = new(1000m);
        LiterMeasurement right = new(2000m);
        left.ShouldBeLessThan(right);
    }

    [Fact]
    public void Operator_GreaterThan_ShouldReturnCorrectResult()
    {
        LiterMeasurement left = new(2000m);
        LiterMeasurement right = new(1000m);
        left.ShouldBeGreaterThan(right);
    }

    [Fact]
    public void Operator_LessThanOrEqual_ShouldReturnCorrectResult()
    {
        LiterMeasurement left = new(1000m);
        LiterMeasurement right = new(2000m);
        left.ShouldBeLessThanOrEqualTo(right);
    }

    [Fact]
    public void Operator_GreaterThanOrEqual_ShouldReturnCorrectResult()
    {
        LiterMeasurement left = new(2000m);
        LiterMeasurement right = new(1000m);
        left.ShouldBeGreaterThanOrEqualTo(right);
    }
}
