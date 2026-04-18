using CSharpFunctionalExtensions;
using Menlo.Lib.Auth.Entities;
using Menlo.Lib.Auth.Errors;
using Menlo.Lib.Auth.Events;
using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Common.Enums;
using Menlo.Lib.Common.ValueObjects;
using Shouldly;

namespace Menlo.Lib.Tests.Auth.Entities;

/// <summary>
/// Tests for Household aggregate root.
/// </summary>
public sealed class HouseholdTests
{
    [Fact]
    public void GivenValidName_WhenCreatingHousehold()
    {
        string name = "Smith Family";

        Result<Household, HouseholdError> result = Household.Create(name);

        ItShouldSucceed(result);
        ItShouldHaveName(result, name);
        ItShouldHaveValidId(result);
        ItShouldRaiseCreatedEvent(result, name);
    }

    [Fact]
    public void GivenNameWithWhitespace_WhenCreatingHousehold()
    {
        string name = "  Smith Family  ";

        Result<Household, HouseholdError> result = Household.Create(name);

        ItShouldSucceed(result);
        ItShouldHaveName(result, "Smith Family");
    }

    [Fact]
    public void GivenEmptyName_WhenCreatingHousehold()
    {
        Result<Household, HouseholdError> result = Household.Create("");

        ItShouldFail(result);
        ItShouldBeInvalidHouseholdDataError(result);
        ItShouldHaveReasonContaining(result, "name");
    }

    [Fact]
    public void GivenWhitespaceName_WhenCreatingHousehold()
    {
        Result<Household, HouseholdError> result = Household.Create("   ");

        ItShouldFail(result);
        ItShouldBeInvalidHouseholdDataError(result);
        ItShouldHaveReasonContaining(result, "name");
    }

    [Fact]
    public void GivenValidHousehold_WhenAuditingForCreate()
    {
        Result<Household, HouseholdError> createResult = Household.Create("Smith Family");
        Household household = createResult.Value;
        IAuditStampFactory factory = CreateAuditStampFactory();

        household.Audit(factory, AuditOperation.Create);

        household.CreatedBy.ShouldNotBeNull();
        household.CreatedAt.ShouldNotBeNull();
        household.ModifiedBy.ShouldNotBeNull();
        household.ModifiedAt.ShouldNotBeNull();
    }

    [Fact]
    public void GivenValidHousehold_WhenAuditingForUpdate()
    {
        Result<Household, HouseholdError> createResult = Household.Create("Smith Family");
        Household household = createResult.Value;
        IAuditStampFactory factory = CreateAuditStampFactory();

        household.Audit(factory, AuditOperation.Update);

        household.CreatedBy.ShouldBeNull();
        household.CreatedAt.ShouldBeNull();
        household.ModifiedBy.ShouldNotBeNull();
        household.ModifiedAt.ShouldNotBeNull();
    }

    [Fact]
    public void GivenValidHousehold_WhenClearingDomainEvents()
    {
        Result<Household, HouseholdError> createResult = Household.Create("Smith Family");
        Household household = createResult.Value;

        household.ClearDomainEvents();

        household.DomainEvents.ShouldBeEmpty();
    }

    // Assertion helpers
    private static void ItShouldSucceed(Result<Household, HouseholdError> result)
        => result.IsSuccess.ShouldBeTrue();

    private static void ItShouldFail(Result<Household, HouseholdError> result)
        => result.IsFailure.ShouldBeTrue();

    private static void ItShouldHaveName(Result<Household, HouseholdError> result, string expectedName)
        => result.Value.Name.ShouldBe(expectedName);

    private static void ItShouldHaveValidId(Result<Household, HouseholdError> result)
        => result.Value.Id.Value.ShouldNotBe(Guid.Empty);

    private static void ItShouldRaiseCreatedEvent(Result<Household, HouseholdError> result, string expectedName)
    {
        Household household = result.Value;
        household.DomainEvents.Count.ShouldBe(1);
        IDomainEvent domainEvent = household.DomainEvents.First();
        domainEvent.ShouldBeOfType<HouseholdCreatedEvent>();
        HouseholdCreatedEvent createdEvent = (HouseholdCreatedEvent)domainEvent;
        createdEvent.HouseholdId.ShouldBe(household.Id);
        createdEvent.Name.ShouldBe(expectedName);
    }

    private static void ItShouldBeInvalidHouseholdDataError(Result<Household, HouseholdError> result)
        => result.Error.ShouldBeOfType<InvalidHouseholdDataError>();

    private static void ItShouldHaveReasonContaining(Result<Household, HouseholdError> result, string expectedSubstring)
    {
        InvalidHouseholdDataError error = (InvalidHouseholdDataError)result.Error;
        error.Reason.ToLowerInvariant().ShouldContain(expectedSubstring.ToLowerInvariant());
    }

    // Test setup helpers
    private sealed class FakeAuditStampFactory(AuditStamp stamp) : IAuditStampFactory
    {
        private readonly AuditStamp _stamp = stamp;
        public AuditStamp CreateStamp() => _stamp;
    }

    private static IAuditStampFactory CreateAuditStampFactory()
    {
        UserId actorId = UserId.NewId();
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;
        return new FakeAuditStampFactory(new AuditStamp(actorId, timestamp));
    }
}
