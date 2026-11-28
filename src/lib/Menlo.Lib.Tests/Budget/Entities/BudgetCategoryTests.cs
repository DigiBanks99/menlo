using CSharpFunctionalExtensions;
using Menlo.Lib.Budget.Entities;
using Menlo.Lib.Budget.Errors;
using Menlo.Lib.Budget.Events;
using Menlo.Lib.Budget.ValueObjects;
using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Common.Enums;
using Menlo.Lib.Common.ValueObjects;
using Shouldly;
using BudgetAggregate = Menlo.Lib.Budget.Entities.Budget;

namespace Menlo.Lib.Tests.Budget.Entities;

public sealed class BudgetCategoryTests
{
    [Test]
    public void ATopLevelCategoryIsAdded()
    {
        BudgetAggregate budget = NewBudget();

        Result<Category, BudgetError> result = budget.AddCategory("Home", parentId: null);

        ItShouldBeAdded();
        ItShouldBeTopLevel();
        ItShouldBelongToTheBudget();
        ItShouldAnnounceIt();

        void ItShouldBeAdded() => result.ShouldSatisfyAllConditions(
            r => r.IsSuccess.ShouldBeTrue(),
            r => r.Value.Name.ShouldBe("Home"),
            r => r.Value.Id.Value.ShouldNotBe(Guid.Empty),
            r => budget.Categories.ShouldHaveSingleItem().ShouldBeSameAs(r.Value));

        void ItShouldBeTopLevel() => result.Value.ShouldSatisfyAllConditions(
            c => c.ParentId.ShouldBeNull(),
            c => c.IsTopLevel.ShouldBeTrue());

        void ItShouldBelongToTheBudget() => result.Value.BudgetId.ShouldBe(budget.Id);

        void ItShouldAnnounceIt() => budget.DomainEvents
            .ShouldHaveSingleItem()
            .ShouldBe(new CategoryAddedEvent(budget.Id, result.Value.Id, "Home", null));
    }

    [Test]
    public void ASubcategoryIsAddedUnderATopLevelCategory()
    {
        BudgetAggregate budget = NewBudget();
        Category home = budget.AddCategory("Home", parentId: null).Value;

        Result<Category, BudgetError> result = budget.AddCategory("Electricity", home.Id);

        ItShouldBeAdded();
        ItShouldSitUnderItsParent();

        void ItShouldBeAdded() => result.IsSuccess.ShouldBeTrue();

        void ItShouldSitUnderItsParent() => result.Value.ShouldSatisfyAllConditions(
            c => c.ParentId.ShouldBe(home.Id),
            c => c.IsTopLevel.ShouldBeFalse(),
            c => budget.Categories.ShouldBe([home, c]));
    }

    [Test]
    public void ACategoryIsAddedUnderASubcategory()
    {
        BudgetAggregate budget = NewBudget();
        Category home = budget.AddCategory("Home", parentId: null).Value;
        Category electricity = budget.AddCategory("Electricity", home.Id).Value;

        Result<Category, BudgetError> result = budget.AddCategory("Prepaid", electricity.Id);

        ItShouldBeRefusedAsAThirdLevel();
        ItShouldLeaveTheTreeAsItWas();

        void ItShouldBeRefusedAsAThirdLevel() => result.Error.ShouldBeOfType<CategoryTooDeepError>().ShouldSatisfyAllConditions(
            e => e.ParentId.ShouldBe(electricity.Id),
            e => e.Code.ShouldBe("Budget.CategoryTooDeep"));

        void ItShouldLeaveTheTreeAsItWas() => budget.Categories.ShouldBe([home, electricity]);
    }

    [Test]
    public void ACategoryIsAddedUnderOneTheBudgetDoesNotHave()
    {
        BudgetAggregate budget = NewBudget();
        CategoryId unknown = CategoryId.NewId();

        Result<Category, BudgetError> result = budget.AddCategory("Electricity", unknown);

        ItShouldBeRefused();
        ItShouldAddNothing();

        void ItShouldBeRefused() => result.Error.ShouldBeOfType<CategoryNotFoundError>().ShouldSatisfyAllConditions(
            e => e.CategoryId.ShouldBe(unknown),
            e => e.Code.ShouldBe("Budget.CategoryNotFound"));

        void ItShouldAddNothing() => budget.Categories.ShouldBeEmpty();
    }

    [Test]
    [Arguments("home")]
    [Arguments("HOME")]
    [Arguments("  Home  ")]
    public void ATopLevelCategoryIsAddedWithASiblingsName(string name)
    {
        BudgetAggregate budget = NewBudget();
        budget.AddCategory("Home", parentId: null);

        Result<Category, BudgetError> result = budget.AddCategory(name, parentId: null);

        ItShouldBeRefusedAsTaken();
        ItShouldKeepOneCategory();

        void ItShouldBeRefusedAsTaken() => result.Error.ShouldBeOfType<CategoryNameTakenError>().ShouldSatisfyAllConditions(
            e => e.Name.ShouldBe(name.Trim()),
            e => e.Code.ShouldBe("Budget.CategoryNameTaken"));

        void ItShouldKeepOneCategory() => budget.Categories.Count.ShouldBe(1);
    }

    [Test]
    public void ASubcategoryIsAddedWithASiblingsName()
    {
        BudgetAggregate budget = NewBudget();
        Category home = budget.AddCategory("Home", parentId: null).Value;
        budget.AddCategory("Electricity", home.Id);

        Result<Category, BudgetError> result = budget.AddCategory("electricity", home.Id);

        ItShouldBeRefusedAsTaken();

        void ItShouldBeRefusedAsTaken() => result.Error.ShouldBeOfType<CategoryNameTakenError>();
    }

    [Test]
    public void CategoriesInDifferentPlacesShareAName()
    {
        BudgetAggregate budget = NewBudget();
        Category home = budget.AddCategory("Home", parentId: null).Value;
        Category holiday = budget.AddCategory("Holiday", parentId: null).Value;

        Result<Category, BudgetError> underHome = budget.AddCategory("Insurance", home.Id);
        Result<Category, BudgetError> underHoliday = budget.AddCategory("Insurance", holiday.Id);
        Result<Category, BudgetError> topLevel = budget.AddCategory("Insurance", parentId: null);

        ItShouldAddEach();

        void ItShouldAddEach() => new[] { underHome, underHoliday, topLevel }.ShouldAllBe(r => r.IsSuccess);
    }

    [Test]
    public void ASubcategoryIsAddedWithItsParentsName()
    {
        BudgetAggregate budget = NewBudget();
        Category home = budget.AddCategory("Home", parentId: null).Value;

        Result<Category, BudgetError> result = budget.AddCategory("Home", home.Id);

        ItShouldBeAdded();

        void ItShouldBeAdded() => result.IsSuccess.ShouldBeTrue();
    }

    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("   ")]
    public void ACategoryIsAddedWithoutAName(string? name)
    {
        BudgetAggregate budget = NewBudget();

        Result<Category, BudgetError> result = budget.AddCategory(name, parentId: null);

        ItShouldBeRefused();
        ItShouldAddNothing();

        void ItShouldBeRefused() => result.Error.ShouldBeOfType<InvalidCategoryNameError>().ShouldSatisfyAllConditions(
            e => e.Name.ShouldBe(name),
            e => e.Code.ShouldBe("Budget.InvalidCategoryName"));

        void ItShouldAddNothing() => budget.Categories.ShouldBeEmpty();
    }

    [Test]
    public void ACategoryIsAddedWithANameTooLong()
    {
        BudgetAggregate budget = NewBudget();

        Result<Category, BudgetError> longest = budget.AddCategory(new string('a', Category.MaxNameLength), parentId: null);
        Result<Category, BudgetError> tooLong = budget.AddCategory(new string('b', Category.MaxNameLength + 1), parentId: null);

        ItShouldTakeTheLongestName();
        ItShouldRefuseALongerOne();

        void ItShouldTakeTheLongestName() => longest.IsSuccess.ShouldBeTrue();

        void ItShouldRefuseALongerOne() => tooLong.Error.ShouldBeOfType<InvalidCategoryNameError>();
    }

    [Test]
    public void ACategoryIsAddedWithSpacesAroundItsName()
    {
        BudgetAggregate budget = NewBudget();

        Result<Category, BudgetError> result = budget.AddCategory("  School fees ", parentId: null);

        ItShouldDropTheSpaces();

        void ItShouldDropTheSpaces() => result.Value.Name.ShouldBe("School fees");
    }

    [Test]
    public void TwoCategoriesAreAdded()
    {
        BudgetAggregate budget = NewBudget();

        Category home = budget.AddCategory("Home", parentId: null).Value;
        Category school = budget.AddCategory("School", parentId: null).Value;

        ItShouldGiveEachItsOwnId();

        void ItShouldGiveEachItsOwnId() => home.Id.ShouldNotBe(school.Id);
    }

    [Test]
    public void ANewCategoryIsSavedAndThenChanged()
    {
        Category category = NewBudget().AddCategory("Home", parentId: null).Value;
        AuditStamp created = new(UserId.NewId(), new DateTimeOffset(2026, 11, 28, 10, 0, 0, TimeSpan.Zero));
        AuditStamp changed = new(UserId.NewId(), new DateTimeOffset(2026, 12, 1, 8, 0, 0, TimeSpan.Zero));

        category.Audit(new FixedAuditStampFactory(created), AuditOperation.Create);
        category.Audit(new FixedAuditStampFactory(changed), AuditOperation.Update);

        ItShouldKeepWhoCreatedIt();
        ItShouldRecordTheChange();

        void ItShouldKeepWhoCreatedIt() => category.ShouldSatisfyAllConditions(
            c => c.CreatedBy.ShouldBe(created.ActorId),
            c => c.CreatedAt.ShouldBe(created.Timestamp));

        void ItShouldRecordTheChange() => category.ShouldSatisfyAllConditions(
            c => c.ModifiedBy.ShouldBe(changed.ActorId),
            c => c.ModifiedAt.ShouldBe(changed.Timestamp));
    }

    private static BudgetAggregate NewBudget()
    {
        BudgetAggregate budget = BudgetAggregate.Create(2027).Value;
        budget.ClearDomainEvents();
        return budget;
    }

    private sealed class FixedAuditStampFactory(AuditStamp stamp) : IAuditStampFactory
    {
        public AuditStamp CreateStamp() => stamp;
    }
}
