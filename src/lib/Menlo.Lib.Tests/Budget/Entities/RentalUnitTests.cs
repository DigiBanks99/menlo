using CSharpFunctionalExtensions;
using Menlo.Lib.Budget.Entities;
using Menlo.Lib.Budget.Errors;
using Menlo.Lib.Budget.Events;
using Menlo.Lib.Budget.ValueObjects;
using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Common.Enums;
using Menlo.Lib.Common.ValueObjects;
using Shouldly;

namespace Menlo.Lib.Tests.Budget.Entities;

public sealed class RentalUnitTests
{
    [Test]
    public void ARentalUnitIsAdded()
    {
        Result<RentalUnit, BudgetError> result = RentalUnit.Create("Garden flat");

        ItShouldBeAdded();
        ItShouldHaveItsName();
        ItShouldHaveAnId();
        ItShouldAnnounceIt();

        void ItShouldBeAdded() => result.IsSuccess.ShouldBeTrue();

        void ItShouldHaveItsName() => result.Value.Name.ShouldBe("Garden flat");

        void ItShouldHaveAnId() => result.Value.Id.Value.ShouldNotBe(Guid.Empty);

        void ItShouldAnnounceIt() => result.Value.DomainEvents
            .ShouldHaveSingleItem()
            .ShouldBe(new RentalUnitCreatedEvent(result.Value.Id, "Garden flat"));
    }

    [Test]
    public void ARentalUnitIsAddedWithSpacesAroundItsName()
    {
        Result<RentalUnit, BudgetError> result = RentalUnit.Create("  Garden flat ");

        ItShouldDropTheSpaces();

        void ItShouldDropTheSpaces() => result.Value.Name.ShouldBe("Garden flat");
    }

    [Test]
    public void ARentalUnitIsAddedWithTheLongestName()
    {
        string name = new('a', RentalUnit.MaxNameLength);

        Result<RentalUnit, BudgetError> result = RentalUnit.Create(name);

        ItShouldBeAdded();

        void ItShouldBeAdded() => result.Value.Name.ShouldBe(name);
    }

    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("   ")]
    public void ARentalUnitIsAddedWithoutAName(string? name)
    {
        Result<RentalUnit, BudgetError> result = RentalUnit.Create(name);

        ItShouldBeRefused();
        ItShouldSayWhy();

        void ItShouldBeRefused() => result.IsFailure.ShouldBeTrue();

        void ItShouldSayWhy() => result.Error.ShouldBeOfType<InvalidRentalUnitNameError>().ShouldSatisfyAllConditions(
            error => error.Name.ShouldBe(name),
            error => error.Code.ShouldBe("RentalUnit.InvalidName"),
            error => error.Message.ShouldBe("A rental unit needs a name of 1 to 100 characters."));
    }

    [Test]
    public void ARentalUnitIsAddedWithANameThatIsTooLong()
    {
        Result<RentalUnit, BudgetError> result = RentalUnit.Create(new string('a', RentalUnit.MaxNameLength + 1));

        ItShouldBeRefused();

        void ItShouldBeRefused() => result.Error.ShouldBeOfType<InvalidRentalUnitNameError>();
    }

    [Test]
    public void TwoRentalUnitsAreAddedWithTheSameName()
    {
        RentalUnit first = RentalUnit.Create("Garden flat").Value;
        RentalUnit second = RentalUnit.Create("Garden flat").Value;

        ItShouldGiveEachItsOwnId();

        void ItShouldGiveEachItsOwnId() => first.Id.ShouldNotBe(second.Id);
    }

    [Test]
    public void ARentalUnitIsRenamed()
    {
        RentalUnit unit = RentalUnit.Create("Garden flat").Value;
        unit.ClearDomainEvents();
        RentalUnitId id = unit.Id;

        UnitResult<BudgetError> result = unit.Rename(" Cottage ");

        ItShouldBeRenamed();
        ItShouldKeepItsIdentity();
        ItShouldAnnounceTheNewName();

        void ItShouldBeRenamed() => result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeTrue(),
            () => unit.Name.ShouldBe("Cottage"));

        void ItShouldKeepItsIdentity() => unit.Id.ShouldBe(id);

        void ItShouldAnnounceTheNewName() => unit.DomainEvents
            .ShouldHaveSingleItem()
            .ShouldBe(new RentalUnitRenamedEvent(id, "Garden flat", "Cottage"));
    }

    [Test]
    public void ARentalUnitIsRenamedToTheNameItHas()
    {
        RentalUnit unit = RentalUnit.Create("Garden flat").Value;
        unit.ClearDomainEvents();

        UnitResult<BudgetError> result = unit.Rename("Garden flat ");

        ItShouldChangeNothing();

        void ItShouldChangeNothing() => result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeTrue(),
            () => unit.Name.ShouldBe("Garden flat"),
            () => unit.DomainEvents.ShouldBeEmpty());
    }

    [Test]
    [Arguments(null)]
    [Arguments(" ")]
    public void ARentalUnitIsRenamedWithoutAName(string? name)
    {
        RentalUnit unit = RentalUnit.Create("Garden flat").Value;
        unit.ClearDomainEvents();

        UnitResult<BudgetError> result = unit.Rename(name);

        ItShouldBeRefused();
        ItShouldKeepItsName();

        void ItShouldBeRefused() => result.Error.ShouldBeOfType<InvalidRentalUnitNameError>();

        void ItShouldKeepItsName() => unit.ShouldSatisfyAllConditions(
            u => u.Name.ShouldBe("Garden flat"),
            u => u.DomainEvents.ShouldBeEmpty());
    }

    [Test]
    public void ARentalUnitIsSavedAndThenChanged()
    {
        RentalUnit unit = RentalUnit.Create("Garden flat").Value;
        AuditStamp created = new(UserId.NewId(), new DateTimeOffset(2026, 11, 28, 10, 0, 0, TimeSpan.Zero));
        AuditStamp changed = new(UserId.NewId(), new DateTimeOffset(2026, 12, 1, 8, 0, 0, TimeSpan.Zero));

        unit.Audit(new FixedAuditStampFactory(created), AuditOperation.Create);
        unit.Audit(new FixedAuditStampFactory(changed), AuditOperation.Update);

        ItShouldKeepWhoAddedIt();
        ItShouldRecordTheChange();

        void ItShouldKeepWhoAddedIt() => unit.ShouldSatisfyAllConditions(
            u => u.CreatedBy.ShouldBe(created.ActorId),
            u => u.CreatedAt.ShouldBe(created.Timestamp));

        void ItShouldRecordTheChange() => unit.ShouldSatisfyAllConditions(
            u => u.ModifiedBy.ShouldBe(changed.ActorId),
            u => u.ModifiedAt.ShouldBe(changed.Timestamp));
    }

    private sealed class FixedAuditStampFactory(AuditStamp stamp) : IAuditStampFactory
    {
        public AuditStamp CreateStamp() => stamp;
    }
}
