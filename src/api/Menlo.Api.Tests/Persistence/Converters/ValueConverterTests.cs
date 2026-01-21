using Menlo.Api.Persistence.Converters;
using Menlo.Lib.Auth.ValueObjects;
using Menlo.Lib.Budget.ValueObjects;
using Menlo.Lib.Common.ValueObjects;
using Shouldly;

namespace Menlo.Api.Tests.Persistence.Converters;

/// <summary>
/// Tests for EF Core value converters.
/// </summary>
public sealed class ValueConverterTests
{
    #region UserIdConverter Tests

    [Fact]
    public void GivenValidUserId_WhenConvertingToDatabase()
    {
        UserIdConverter converter = new();
        Guid guidValue = Guid.NewGuid();
        UserId userId = new(guidValue);

        Guid result = (Guid)converter.ConvertToProvider(userId)!;

        ItShouldConvertToGuid(result, guidValue);
    }

    [Fact]
    public void GivenValidGuid_WhenConvertingToUserId()
    {
        UserIdConverter converter = new();
        Guid guidValue = Guid.NewGuid();

        UserId result = (UserId)converter.ConvertFromProvider(guidValue)!;

        ItShouldConvertToUserId(result, guidValue);
    }

    [Fact]
    public void GivenValidUserId_WhenRoundTripping()
    {
        UserIdConverter converter = new();
        Guid originalGuid = Guid.NewGuid();
        UserId originalUserId = new(originalGuid);

        Guid dbValue = (Guid)converter.ConvertToProvider(originalUserId)!;
        UserId roundTripUserId = (UserId)converter.ConvertFromProvider(dbValue)!;

        ItShouldMatchOriginalUserId(roundTripUserId, originalUserId);
    }

    [Fact]
    public void GivenEmptyGuid_WhenConvertingToUserId()
    {
        UserIdConverter converter = new();
        Guid emptyGuid = Guid.Empty;

        UserId result = (UserId)converter.ConvertFromProvider(emptyGuid)!;

        ItShouldHaveEmptyGuid(result);
    }

    #endregion

    #region NullableUserIdConverter Tests

    [Fact]
    public void GivenValidNullableUserId_WhenConvertingToDatabase()
    {
        NullableUserIdConverter converter = new();
        Guid guidValue = Guid.NewGuid();
        UserId? userId = new UserId(guidValue);

        Guid? result = (Guid?)converter.ConvertToProvider(userId);

        ItShouldConvertToNullableGuid(result, guidValue);
    }

    [Fact]
    public void GivenNullUserId_WhenConvertingToDatabase()
    {
        NullableUserIdConverter converter = new();
        UserId? userId = null;

        Guid? result = (Guid?)converter.ConvertToProvider(userId);

        ItShouldBeNull(result);
    }

    [Fact]
    public void GivenValidNullableGuid_WhenConvertingToNullableUserId()
    {
        NullableUserIdConverter converter = new();
        Guid? guidValue = Guid.NewGuid();

        UserId? result = (UserId?)converter.ConvertFromProvider(guidValue);

        ItShouldConvertToNullableUserId(result, guidValue.Value);
    }

    [Fact]
    public void GivenNullGuid_WhenConvertingToNullableUserId()
    {
        NullableUserIdConverter converter = new();
        Guid? guidValue = null;

        UserId? result = (UserId?)converter.ConvertFromProvider(guidValue);

        ItShouldBeNullUserId(result);
    }

    [Fact]
    public void GivenValidNullableUserId_WhenRoundTripping()
    {
        NullableUserIdConverter converter = new();
        Guid originalGuid = Guid.NewGuid();
        UserId? originalUserId = new UserId(originalGuid);

        Guid? dbValue = (Guid?)converter.ConvertToProvider(originalUserId);
        UserId? roundTripUserId = (UserId?)converter.ConvertFromProvider(dbValue);

        ItShouldMatchOriginalNullableUserId(roundTripUserId, originalUserId);
    }

    [Fact]
    public void GivenNullUserId_WhenRoundTripping()
    {
        NullableUserIdConverter converter = new();
        UserId? originalUserId = null;

        Guid? dbValue = (Guid?)converter.ConvertToProvider(originalUserId);
        UserId? roundTripUserId = (UserId?)converter.ConvertFromProvider(dbValue);

        ItShouldBeNullUserId(roundTripUserId);
    }

    #endregion

    #region ExternalUserIdConverter Tests

    [Fact]
    public void GivenValidExternalUserId_WhenConvertingToDatabase()
    {
        ExternalUserIdConverter converter = new();
        string stringValue = "external-user-123";
        ExternalUserId externalUserId = new(stringValue);

        string result = (string)converter.ConvertToProvider(externalUserId)!;

        ItShouldConvertToString(result, stringValue);
    }

    [Fact]
    public void GivenValidString_WhenConvertingToExternalUserId()
    {
        ExternalUserIdConverter converter = new();
        string stringValue = "external-user-123";

        ExternalUserId result = (ExternalUserId)converter.ConvertFromProvider(stringValue)!;

        ItShouldConvertToExternalUserId(result, stringValue);
    }

    [Fact]
    public void GivenValidExternalUserId_WhenRoundTripping()
    {
        ExternalUserIdConverter converter = new();
        string originalString = "external-user-123";
        ExternalUserId originalExternalUserId = new(originalString);

        string dbValue = (string)converter.ConvertToProvider(originalExternalUserId)!;
        ExternalUserId roundTripExternalUserId = (ExternalUserId)converter.ConvertFromProvider(dbValue)!;

        ItShouldMatchOriginalExternalUserId(roundTripExternalUserId, originalExternalUserId);
    }

    [Fact]
    public void GivenEmptyString_WhenConvertingToExternalUserId()
    {
        ExternalUserIdConverter converter = new();
        string emptyString = string.Empty;

        ExternalUserId result = (ExternalUserId)converter.ConvertFromProvider(emptyString)!;

        ItShouldHaveEmptyString(result);
    }

    #endregion

    #region BudgetIdConverter Tests

    [Fact]
    public void GivenValidBudgetId_WhenConvertingToDatabase()
    {
        BudgetIdConverter converter = new();
        Guid guidValue = Guid.NewGuid();
        BudgetId budgetId = new(guidValue);

        Guid result = (Guid)converter.ConvertToProvider(budgetId)!;

        ItShouldConvertToGuid(result, guidValue);
    }

    [Fact]
    public void GivenValidGuid_WhenConvertingToBudgetId()
    {
        BudgetIdConverter converter = new();
        Guid guidValue = Guid.NewGuid();

        BudgetId result = (BudgetId)converter.ConvertFromProvider(guidValue)!;

        ItShouldConvertToBudgetId(result, guidValue);
    }

    [Fact]
    public void GivenValidBudgetId_WhenRoundTripping()
    {
        BudgetIdConverter converter = new();
        Guid originalGuid = Guid.NewGuid();
        BudgetId originalBudgetId = new(originalGuid);

        Guid dbValue = (Guid)converter.ConvertToProvider(originalBudgetId)!;
        BudgetId roundTripBudgetId = (BudgetId)converter.ConvertFromProvider(dbValue)!;

        ItShouldMatchOriginalBudgetId(roundTripBudgetId, originalBudgetId);
    }

    [Fact]
    public void GivenEmptyGuid_WhenConvertingToBudgetId()
    {
        BudgetIdConverter converter = new();
        Guid emptyGuid = Guid.Empty;

        BudgetId result = (BudgetId)converter.ConvertFromProvider(emptyGuid)!;

        ItShouldHaveEmptyBudgetGuid(result);
    }

    #endregion

    #region NullableBudgetIdConverter Tests

    [Fact]
    public void GivenValidNullableBudgetId_WhenConvertingToDatabase()
    {
        NullableBudgetIdConverter converter = new();
        Guid guidValue = Guid.NewGuid();
        BudgetId? budgetId = new BudgetId(guidValue);

        Guid? result = (Guid?)converter.ConvertToProvider(budgetId);

        ItShouldConvertToNullableGuid(result, guidValue);
    }

    [Fact]
    public void GivenNullBudgetId_WhenConvertingToDatabase()
    {
        NullableBudgetIdConverter converter = new();
        BudgetId? budgetId = null;

        Guid? result = (Guid?)converter.ConvertToProvider(budgetId);

        ItShouldBeNull(result);
    }

    [Fact]
    public void GivenValidNullableGuid_WhenConvertingToNullableBudgetId()
    {
        NullableBudgetIdConverter converter = new();
        Guid? guidValue = Guid.NewGuid();

        BudgetId? result = (BudgetId?)converter.ConvertFromProvider(guidValue);

        ItShouldConvertToNullableBudgetId(result, guidValue.Value);
    }

    [Fact]
    public void GivenNullGuid_WhenConvertingToNullableBudgetId()
    {
        NullableBudgetIdConverter converter = new();
        Guid? guidValue = null;

        BudgetId? result = (BudgetId?)converter.ConvertFromProvider(guidValue);

        ItShouldBeNullBudgetId(result);
    }

    [Fact]
    public void GivenValidNullableBudgetId_WhenRoundTripping()
    {
        NullableBudgetIdConverter converter = new();
        Guid originalGuid = Guid.NewGuid();
        BudgetId? originalBudgetId = new BudgetId(originalGuid);

        Guid? dbValue = (Guid?)converter.ConvertToProvider(originalBudgetId);
        BudgetId? roundTripBudgetId = (BudgetId?)converter.ConvertFromProvider(dbValue);

        ItShouldMatchOriginalNullableBudgetId(roundTripBudgetId, originalBudgetId);
    }

    [Fact]
    public void GivenNullBudgetId_WhenRoundTripping()
    {
        NullableBudgetIdConverter converter = new();
        BudgetId? originalBudgetId = null;

        Guid? dbValue = (Guid?)converter.ConvertToProvider(originalBudgetId);
        BudgetId? roundTripBudgetId = (BudgetId?)converter.ConvertFromProvider(dbValue);

        ItShouldBeNullBudgetId(roundTripBudgetId);
    }

    #endregion

    #region BudgetCategoryIdConverter Tests

    [Fact]
    public void GivenValidBudgetCategoryId_WhenConvertingToDatabase()
    {
        BudgetCategoryIdConverter converter = new();
        Guid guidValue = Guid.NewGuid();
        BudgetCategoryId budgetCategoryId = new(guidValue);

        Guid result = (Guid)converter.ConvertToProvider(budgetCategoryId)!;

        ItShouldConvertToGuid(result, guidValue);
    }

    [Fact]
    public void GivenValidGuid_WhenConvertingToBudgetCategoryId()
    {
        BudgetCategoryIdConverter converter = new();
        Guid guidValue = Guid.NewGuid();

        BudgetCategoryId result = (BudgetCategoryId)converter.ConvertFromProvider(guidValue)!;

        ItShouldConvertToBudgetCategoryId(result, guidValue);
    }

    [Fact]
    public void GivenValidBudgetCategoryId_WhenRoundTripping()
    {
        BudgetCategoryIdConverter converter = new();
        Guid originalGuid = Guid.NewGuid();
        BudgetCategoryId originalBudgetCategoryId = new(originalGuid);

        Guid dbValue = (Guid)converter.ConvertToProvider(originalBudgetCategoryId)!;
        BudgetCategoryId roundTripBudgetCategoryId = (BudgetCategoryId)converter.ConvertFromProvider(dbValue)!;

        ItShouldMatchOriginalBudgetCategoryId(roundTripBudgetCategoryId, originalBudgetCategoryId);
    }

    [Fact]
    public void GivenEmptyGuid_WhenConvertingToBudgetCategoryId()
    {
        BudgetCategoryIdConverter converter = new();
        Guid emptyGuid = Guid.Empty;

        BudgetCategoryId result = (BudgetCategoryId)converter.ConvertFromProvider(emptyGuid)!;

        ItShouldHaveEmptyBudgetCategoryGuid(result);
    }

    #endregion

    #region NullableBudgetCategoryIdConverter Tests

    [Fact]
    public void GivenValidNullableBudgetCategoryId_WhenConvertingToDatabase()
    {
        NullableBudgetCategoryIdConverter converter = new();
        Guid guidValue = Guid.NewGuid();
        BudgetCategoryId? budgetCategoryId = new BudgetCategoryId(guidValue);

        Guid? result = (Guid?)converter.ConvertToProvider(budgetCategoryId);

        ItShouldConvertToNullableGuid(result, guidValue);
    }

    [Fact]
    public void GivenNullBudgetCategoryId_WhenConvertingToDatabase()
    {
        NullableBudgetCategoryIdConverter converter = new();
        BudgetCategoryId? budgetCategoryId = null;

        Guid? result = (Guid?)converter.ConvertToProvider(budgetCategoryId);

        ItShouldBeNull(result);
    }

    [Fact]
    public void GivenValidNullableGuid_WhenConvertingToNullableBudgetCategoryId()
    {
        NullableBudgetCategoryIdConverter converter = new();
        Guid? guidValue = Guid.NewGuid();

        BudgetCategoryId? result = (BudgetCategoryId?)converter.ConvertFromProvider(guidValue);

        ItShouldConvertToNullableBudgetCategoryId(result, guidValue.Value);
    }

    [Fact]
    public void GivenNullGuid_WhenConvertingToNullableBudgetCategoryId()
    {
        NullableBudgetCategoryIdConverter converter = new();
        Guid? guidValue = null;

        BudgetCategoryId? result = (BudgetCategoryId?)converter.ConvertFromProvider(guidValue);

        ItShouldBeNullBudgetCategoryId(result);
    }

    [Fact]
    public void GivenValidNullableBudgetCategoryId_WhenRoundTripping()
    {
        NullableBudgetCategoryIdConverter converter = new();
        Guid originalGuid = Guid.NewGuid();
        BudgetCategoryId? originalBudgetCategoryId = new BudgetCategoryId(originalGuid);

        Guid? dbValue = (Guid?)converter.ConvertToProvider(originalBudgetCategoryId);
        BudgetCategoryId? roundTripBudgetCategoryId = (BudgetCategoryId?)converter.ConvertFromProvider(dbValue);

        ItShouldMatchOriginalNullableBudgetCategoryId(roundTripBudgetCategoryId, originalBudgetCategoryId);
    }

    [Fact]
    public void GivenNullBudgetCategoryId_WhenRoundTripping()
    {
        NullableBudgetCategoryIdConverter converter = new();
        BudgetCategoryId? originalBudgetCategoryId = null;

        Guid? dbValue = (Guid?)converter.ConvertToProvider(originalBudgetCategoryId);
        BudgetCategoryId? roundTripBudgetCategoryId = (BudgetCategoryId?)converter.ConvertFromProvider(dbValue);

        ItShouldBeNullBudgetCategoryId(roundTripBudgetCategoryId);
    }

    #endregion

    #region Assertion Helpers - UserId

    private static void ItShouldConvertToGuid(Guid result, Guid expected)
    {
        result.ShouldBe(expected);
    }

    private static void ItShouldConvertToUserId(UserId result, Guid expectedGuid)
    {
        result.Value.ShouldBe(expectedGuid);
    }

    private static void ItShouldMatchOriginalUserId(UserId result, UserId original)
    {
        result.ShouldBe(original);
        result.Value.ShouldBe(original.Value);
    }

    private static void ItShouldHaveEmptyGuid(UserId result)
    {
        result.Value.ShouldBe(Guid.Empty);
    }

    private static void ItShouldConvertToNullableGuid(Guid? result, Guid expected)
    {
        result.ShouldNotBeNull();
        result.Value.ShouldBe(expected);
    }

    private static void ItShouldBeNull(Guid? result)
    {
        result.ShouldBeNull();
    }

    private static void ItShouldConvertToNullableUserId(UserId? result, Guid expectedGuid)
    {
        result.ShouldNotBeNull();
        result.Value.Value.ShouldBe(expectedGuid);
    }

    private static void ItShouldBeNullUserId(UserId? result)
    {
        result.ShouldBeNull();
    }

    private static void ItShouldMatchOriginalNullableUserId(UserId? result, UserId? original)
    {
        result.ShouldBe(original);
        if (original.HasValue)
        {
            result!.Value.Value.ShouldBe(original.Value.Value);
        }
    }

    #endregion

    #region Assertion Helpers - ExternalUserId

    private static void ItShouldConvertToString(string result, string expected)
    {
        result.ShouldBe(expected);
    }

    private static void ItShouldConvertToExternalUserId(ExternalUserId result, string expectedString)
    {
        result.Value.ShouldBe(expectedString);
    }

    private static void ItShouldMatchOriginalExternalUserId(ExternalUserId result, ExternalUserId original)
    {
        result.ShouldBe(original);
        result.Value.ShouldBe(original.Value);
    }

    private static void ItShouldHaveEmptyString(ExternalUserId result)
    {
        result.Value.ShouldBe(string.Empty);
    }

    #endregion

    #region Assertion Helpers - BudgetId

    private static void ItShouldConvertToBudgetId(BudgetId result, Guid expectedGuid)
    {
        result.Value.ShouldBe(expectedGuid);
    }

    private static void ItShouldMatchOriginalBudgetId(BudgetId result, BudgetId original)
    {
        result.ShouldBe(original);
        result.Value.ShouldBe(original.Value);
    }

    private static void ItShouldHaveEmptyBudgetGuid(BudgetId result)
    {
        result.Value.ShouldBe(Guid.Empty);
    }

    private static void ItShouldConvertToNullableBudgetId(BudgetId? result, Guid expectedGuid)
    {
        result.ShouldNotBeNull();
        result.Value.Value.ShouldBe(expectedGuid);
    }

    private static void ItShouldBeNullBudgetId(BudgetId? result)
    {
        result.ShouldBeNull();
    }

    private static void ItShouldMatchOriginalNullableBudgetId(BudgetId? result, BudgetId? original)
    {
        result.ShouldBe(original);
        if (original.HasValue)
        {
            result!.Value.Value.ShouldBe(original.Value.Value);
        }
    }

    #endregion

    #region Assertion Helpers - BudgetCategoryId

    private static void ItShouldConvertToBudgetCategoryId(BudgetCategoryId result, Guid expectedGuid)
    {
        result.Value.ShouldBe(expectedGuid);
    }

    private static void ItShouldMatchOriginalBudgetCategoryId(BudgetCategoryId result, BudgetCategoryId original)
    {
        result.ShouldBe(original);
        result.Value.ShouldBe(original.Value);
    }

    private static void ItShouldHaveEmptyBudgetCategoryGuid(BudgetCategoryId result)
    {
        result.Value.ShouldBe(Guid.Empty);
    }

    private static void ItShouldConvertToNullableBudgetCategoryId(BudgetCategoryId? result, Guid expectedGuid)
    {
        result.ShouldNotBeNull();
        result.Value.Value.ShouldBe(expectedGuid);
    }

    private static void ItShouldBeNullBudgetCategoryId(BudgetCategoryId? result)
    {
        result.ShouldBeNull();
    }

    private static void ItShouldMatchOriginalNullableBudgetCategoryId(BudgetCategoryId? result, BudgetCategoryId? original)
    {
        result.ShouldBe(original);
        if (original.HasValue)
        {
            result!.Value.Value.ShouldBe(original.Value.Value);
        }
    }

    #endregion
}
