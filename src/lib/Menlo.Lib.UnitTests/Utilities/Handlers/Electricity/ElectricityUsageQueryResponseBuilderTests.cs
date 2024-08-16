using Menlo.Utilities.Models;

namespace Menlo.Utilities.Handlers.Electricity;

[Trait("Category", "Unit")]
[Trait("Module", "Utilities")]
public class ElectricityUsageQueryResponseBuilderTests
{
    [Fact]
    public void Build_ShouldAccountForPurchases()
    {
        // Arrange
        ElectricityUsage currentUsageRecord = new()
        {
            Date = DateTimeOffset.Now,
            Units = 795.98m
        };
        ElectricityUsage previousUsageRecord = new()
        {
            Date = DateTimeOffset.Now.AddDays(-1),
            Units = 138.22m
        };
        ElectricityPurchase purchaseRecord = new()
        {
            Date = DateTimeOffset.Now,
            Units = 685.00m,
            Cost = 2500m
        };
        ElectricityUsageQueryResponseBuilder builder = new()
        {
            CurrentUsageRecord = currentUsageRecord,
            PreviousUsageRecord = previousUsageRecord,
            PurchaseRecord = purchaseRecord
        };

        // Act
        ElectricityUsageQueryResponse result = builder.Build();

        // Assert
        result.ShouldBeAssignableTo<ElectricityUsageQueryResponse>();
        result.Date.ShouldBe(currentUsageRecord.Date);
        result.Units.ShouldBe(795.98m);
        result.Usage.ShouldBe(27.24m);
        result.ApplianceUsages.ShouldBeEmpty();
    }

    [Fact]
    public void Build_ShouldReturnUnitsIfNoPreviousPurchase()
    {
        // Arrange
        ElectricityUsage currentUsageRecord = new()
        {
            Date = DateTimeOffset.Now,
            Units = 795.98m
        };
        ElectricityUsageQueryResponseBuilder builder = new()
        {
            CurrentUsageRecord = currentUsageRecord
        };

        // Act
        ElectricityUsageQueryResponse result = builder.Build();

        // Assert
        result.ShouldBeAssignableTo<ElectricityUsageQueryResponse>();
        result.Date.ShouldBe(currentUsageRecord.Date);
        result.Units.ShouldBe(795.98m);
        result.Usage.ShouldBe(795.98m);
        result.ApplianceUsages.ShouldBeEmpty();
    }
}
