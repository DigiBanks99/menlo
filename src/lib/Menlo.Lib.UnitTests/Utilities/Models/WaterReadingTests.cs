using JetBrains.Annotations;

namespace Menlo.Utilities.Models;

[TestSubject(typeof(WaterReading))]
public class WaterReadingTests
{
    [Fact]
    public void Cost_ShouldReturnTheMunicipalReading_WhenMunicipalReadingHasBeenTaken()
    {
        // Arrange
        WaterReading.Reading ownReading = new(
            new LiterMeasurement(28999m),
            new LiterMeasurement(32451),
            new Money(1200, Currency.Zar));
        WaterReading waterReading = new(ownReading, new DateOnly(2024, 8, 1));

        // Act
        WaterReading.Reading municipalReading = new(
            new LiterMeasurement(26979m),
            new LiterMeasurement(33451),
            new Money(1500, Currency.Zar));
        waterReading.TakeMunicipalReading(municipalReading);

        // Assert
        waterReading.Cost.ShouldBe(municipalReading.Cost);
    }

    [Fact]
    public void Cost_ShouldReturnTheOwnReading_WhenMunicipalReadingHasNotBeenTaken()
    {
        WaterReading.Reading ownReading = new(
            new LiterMeasurement(28999m),
            new LiterMeasurement(32451),
            new Money(1200, Currency.Zar));
        WaterReading waterReading = new(ownReading, new DateOnly(2024, 8, 1));

        waterReading.Cost.ShouldBe(ownReading.Cost);
    }

    [Fact]
    public void Usage_ShouldReturnTheMunicipalReading_WhenMunicipalReadingHasBeenTaken()
    {
        // Arrange
        WaterReading.Reading ownReading = new(
            new LiterMeasurement(28999m),
            new LiterMeasurement(32451),
            new Money(1200, Currency.Zar));
        WaterReading waterReading = new(ownReading, new DateOnly(2024, 8, 1));

        // Act
        WaterReading.Reading municipalReading = new(
            new LiterMeasurement(26979m),
            new LiterMeasurement(33451),
            new Money(1500, Currency.Zar));
        waterReading.TakeMunicipalReading(municipalReading);

        // Assert
        waterReading.Usage.ShouldBe(municipalReading.Usage);
    }

    [Fact]
    public void Usage_ShouldReturnTheOwnReading_WhenMunicipalReadingHasNotBeenTaken()
    {
        WaterReading.Reading ownReading = new(
            new LiterMeasurement(28999m),
            new LiterMeasurement(32451),
            new Money(1200, Currency.Zar));
        WaterReading waterReading = new(ownReading, new DateOnly(2024, 8, 1));

        waterReading.Usage.ShouldBe(ownReading.Usage);
    }

    [Fact]
    public void UpdateOwnReading_ShouldUpdateTheOwnReading()
    {
        // Arrange
        WaterReading.Reading ownReading = new(
            new LiterMeasurement(28999m),
            new LiterMeasurement(32451),
            new Money(1200, Currency.Zar));
        WaterReading waterReading = new(ownReading, new DateOnly(2024, 8, 1));

        // Act
        WaterReading.Reading newOwnReading = new(
            new LiterMeasurement(28999m),
            new LiterMeasurement(32451),
            new Money(1500, Currency.Zar));

        waterReading.UpdateOwnReading(newOwnReading);

        // Assert
        waterReading.OwnReading.ShouldBe(newOwnReading);
    }

    [Fact]
    public void TakeMunicipalReading_ShouldTakeTheMunicipalReading()
    {
        // Arrange
        WaterReading.Reading ownReading = new(
            new LiterMeasurement(28999m),
            new LiterMeasurement(32451),
            new Money(1200, Currency.Zar));
        WaterReading waterReading = new(ownReading, new DateOnly(2024, 8, 1));

        // Act
        WaterReading.Reading municipalReading = new(
            new LiterMeasurement(26979m),
            new LiterMeasurement(33451),
            new Money(1500, Currency.Zar));
        waterReading.TakeMunicipalReading(municipalReading);

        // Assert
        waterReading.MunicipalReading.ShouldBe(municipalReading);
    }
}
