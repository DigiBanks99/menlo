using CSharpFunctionalExtensions;
using Menlo.Lib.Budget.Errors;
using Menlo.Lib.Budget.Events;
using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Common.Enums;
using Menlo.Lib.Common.ValueObjects;
using Shouldly;
using BudgetAggregate = Menlo.Lib.Budget.Entities.Budget;

namespace Menlo.Lib.Tests.Budget.Entities;

public sealed class BudgetTests
{
    [Test]
    [Arguments(2027)]
    [Arguments(BudgetAggregate.MinYear)]
    [Arguments(BudgetAggregate.MaxYear)]
    public void ABudgetIsCreatedForAYear(int year)
    {
        Result<BudgetAggregate, BudgetError> result = BudgetAggregate.Create(year);

        ItShouldBeCreated();
        ItShouldCoverTheYear();
        ItShouldHaveAnId();
        ItShouldAnnounceItsCreation();

        void ItShouldBeCreated() => result.IsSuccess.ShouldBeTrue();

        void ItShouldCoverTheYear() => result.Value.Year.ShouldBe(year);

        void ItShouldHaveAnId() => result.Value.Id.Value.ShouldNotBe(Guid.Empty);

        void ItShouldAnnounceItsCreation() => result.Value.DomainEvents
            .ShouldHaveSingleItem()
            .ShouldBe(new BudgetCreatedEvent(result.Value.Id, year));
    }

    [Test]
    [Arguments(0)]
    [Arguments(-2026)]
    [Arguments(10000)]
    public void ABudgetIsCreatedForAYearThatCantBeABudgetYear(int year)
    {
        Result<BudgetAggregate, BudgetError> result = BudgetAggregate.Create(year);

        ItShouldBeRefused();
        ItShouldSayWhy();

        void ItShouldBeRefused() => result.IsFailure.ShouldBeTrue();

        void ItShouldSayWhy() => result.Error.ShouldBeOfType<InvalidBudgetYearError>().ShouldSatisfyAllConditions(
            error => error.Year.ShouldBe(year),
            error => error.Code.ShouldBe("Budget.InvalidYear"));
    }

    [Test]
    public void TwoBudgetsAreCreated()
    {
        BudgetAggregate first = BudgetAggregate.Create(2027).Value;
        BudgetAggregate second = BudgetAggregate.Create(2028).Value;

        ItShouldGiveEachItsOwnId();

        void ItShouldGiveEachItsOwnId() => first.Id.ShouldNotBe(second.Id);
    }

    [Test]
    public void ANewBudgetIsSaved()
    {
        BudgetAggregate budget = BudgetAggregate.Create(2027).Value;
        AuditStamp stamp = new(UserId.NewId(), new DateTimeOffset(2026, 11, 28, 10, 0, 0, TimeSpan.Zero));

        budget.Audit(new FixedAuditStampFactory(stamp), AuditOperation.Create);

        ItShouldRecordWhoCreatedItAndWhen();
        ItShouldRecordTheSameAsItsLastChange();

        void ItShouldRecordWhoCreatedItAndWhen() => budget.ShouldSatisfyAllConditions(
            b => b.CreatedBy.ShouldBe(stamp.ActorId),
            b => b.CreatedAt.ShouldBe(stamp.Timestamp));

        void ItShouldRecordTheSameAsItsLastChange() => budget.ShouldSatisfyAllConditions(
            b => b.ModifiedBy.ShouldBe(stamp.ActorId),
            b => b.ModifiedAt.ShouldBe(stamp.Timestamp));
    }

    [Test]
    public void ASavedBudgetIsChanged()
    {
        BudgetAggregate budget = BudgetAggregate.Create(2027).Value;
        AuditStamp created = new(UserId.NewId(), new DateTimeOffset(2026, 11, 28, 10, 0, 0, TimeSpan.Zero));
        AuditStamp changed = new(UserId.NewId(), new DateTimeOffset(2026, 12, 1, 8, 0, 0, TimeSpan.Zero));

        budget.Audit(new FixedAuditStampFactory(created), AuditOperation.Create);
        budget.Audit(new FixedAuditStampFactory(changed), AuditOperation.Update);

        ItShouldKeepWhoCreatedIt();
        ItShouldRecordTheChange();

        void ItShouldKeepWhoCreatedIt() => budget.ShouldSatisfyAllConditions(
            b => b.CreatedBy.ShouldBe(created.ActorId),
            b => b.CreatedAt.ShouldBe(created.Timestamp));

        void ItShouldRecordTheChange() => budget.ShouldSatisfyAllConditions(
            b => b.ModifiedBy.ShouldBe(changed.ActorId),
            b => b.ModifiedAt.ShouldBe(changed.Timestamp));
    }

    [Test]
    public void ItsEventsAreCleared()
    {
        BudgetAggregate budget = BudgetAggregate.Create(2027).Value;

        budget.ClearDomainEvents();

        ItShouldHaveNoEventsLeft();

        void ItShouldHaveNoEventsLeft() => budget.DomainEvents.ShouldBeEmpty();
    }

    [Test]
    public void AYearAlreadyHasABudget()
    {
        BudgetYearTakenError error = new(2027);

        ItShouldNameTheYear();

        void ItShouldNameTheYear() => error.ShouldSatisfyAllConditions(
            e => e.Year.ShouldBe(2027),
            e => e.Code.ShouldBe("Budget.YearTaken"),
            e => e.Message.ShouldBe("2027 already has a budget."));
    }

    private sealed class FixedAuditStampFactory(AuditStamp stamp) : IAuditStampFactory
    {
        public AuditStamp CreateStamp() => stamp;
    }
}
