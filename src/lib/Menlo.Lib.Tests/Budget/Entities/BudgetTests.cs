using CSharpFunctionalExtensions;
using Menlo.Lib.Budget.Entities;
using Menlo.Lib.Budget.Enums;
using Menlo.Lib.Budget.Errors;
using Menlo.Lib.Budget.Events;
using Menlo.Lib.Budget.ValueObjects;
using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Common.ValueObjects;
using Shouldly;

namespace Menlo.Lib.Tests.Budget.Entities;

/// <summary>
/// Tests for the Budget aggregate root.
/// </summary>
public sealed class BudgetTests
{
    // -------------------------------------------------------------------------
    // Budget.Create
    // -------------------------------------------------------------------------

    [Fact]
    public void GivenValidHouseholdAndYear_WhenCreatingBudget()
    {
        HouseholdId householdId = HouseholdId.NewId();
        int year = 2025;
        IAuditStampFactory factory = CreateAuditStampFactory();

        Result<Menlo.Lib.Budget.Entities.Budget, BudgetError> result =
            Menlo.Lib.Budget.Entities.Budget.Create(householdId, year, factory);

        ItShouldSucceed(result);
        ItShouldHaveHouseholdId(result, householdId);
        ItShouldHaveYear(result, year);
        ItShouldHaveEmptyCategories(result);
        ItShouldHaveAuditStampsSet(result);
        ItShouldRaiseBudgetCreatedEvent(result, year);
    }

    [Fact]
    public void GivenYearOfZero_WhenCreatingBudget()
    {
        HouseholdId householdId = HouseholdId.NewId();
        IAuditStampFactory factory = CreateAuditStampFactory();

        Result<Menlo.Lib.Budget.Entities.Budget, BudgetError> result =
            Menlo.Lib.Budget.Entities.Budget.Create(householdId, 0, factory);

        ItShouldFail(result);
        ItShouldBeInvalidBudgetDataError(result);
    }

    [Fact]
    public void GivenNegativeYear_WhenCreatingBudget()
    {
        HouseholdId householdId = HouseholdId.NewId();
        IAuditStampFactory factory = CreateAuditStampFactory();

        Result<Menlo.Lib.Budget.Entities.Budget, BudgetError> result =
            Menlo.Lib.Budget.Entities.Budget.Create(householdId, -1, factory);

        ItShouldFail(result);
        ItShouldBeInvalidBudgetDataError(result);
    }

    [Fact]
    public void GivenValidYear_WhenCreatingBudget_StatusIsDraft()
    {
        HouseholdId householdId = HouseholdId.NewId();
        IAuditStampFactory factory = CreateAuditStampFactory();

        Result<Menlo.Lib.Budget.Entities.Budget, BudgetError> result =
            Menlo.Lib.Budget.Entities.Budget.Create(householdId, 2025, factory);

        ItShouldSucceed(result);
        ItShouldHaveStatus(result, BudgetStatus.Draft);
    }

    [Fact]
    public void GivenValidYear_WhenCreatingBudget_TotalPlannedMonthlyAmountIsZero()
    {
        HouseholdId householdId = HouseholdId.NewId();
        IAuditStampFactory factory = CreateAuditStampFactory();

        Result<Menlo.Lib.Budget.Entities.Budget, BudgetError> result =
            Menlo.Lib.Budget.Entities.Budget.Create(householdId, 2025, factory);

        ItShouldSucceed(result);
        ItShouldHaveTotalPlannedMonthlyAmount(result, 0m, "ZAR");
    }

    // -------------------------------------------------------------------------
    // Budget.AddCategory
    // -------------------------------------------------------------------------

    [Fact]
    public void GivenValidName_WhenAddingCategory()
    {
        Menlo.Lib.Budget.Entities.Budget budget = CreateDraftBudget();

        Result<CategoryNode, BudgetError> result = budget.AddCategory("Groceries");

        ItShouldSucceedCategoryResult(result);
        ItShouldHaveCategoryName(result, "Groceries");
        ItShouldHaveValidCategoryId(result);
        ItShouldRaiseBudgetCategoryAddedEvent(budget, result.Value, null);
    }

    [Fact]
    public void GivenNameWithWhitespace_WhenAddingCategory()
    {
        Menlo.Lib.Budget.Entities.Budget budget = CreateDraftBudget();

        Result<CategoryNode, BudgetError> result = budget.AddCategory("  Groceries  ");

        ItShouldSucceedCategoryResult(result);
        ItShouldHaveCategoryName(result, "Groceries");
    }

    [Fact]
    public void GivenEmptyName_WhenAddingCategory()
    {
        Menlo.Lib.Budget.Entities.Budget budget = CreateDraftBudget();

        Result<CategoryNode, BudgetError> result = budget.AddCategory("");

        ItShouldFailCategoryResult(result);
        ItShouldBeCategoryInvalidBudgetDataError(result);
    }

    [Fact]
    public void GivenDuplicateSiblingName_WhenAddingCategory()
    {
        Menlo.Lib.Budget.Entities.Budget budget = CreateDraftBudget();
        budget.AddCategory("Groceries");

        Result<CategoryNode, BudgetError> result = budget.AddCategory("GROCERIES");

        ItShouldFailCategoryResult(result);
        ItShouldBeDuplicateCategoryNameError(result, "GROCERIES");
    }

    [Fact]
    public void GivenDuplicateNameUnderDifferentParent_WhenAddingCategory()
    {
        Menlo.Lib.Budget.Entities.Budget budget = CreateDraftBudget();
        Result<CategoryNode, BudgetError> parent1Result = budget.AddCategory("Food");
        Result<CategoryNode, BudgetError> parent2Result = budget.AddCategory("Transport");

        budget.AddCategory("Groceries", parent1Result.Value.Id);
        Result<CategoryNode, BudgetError> result = budget.AddCategory("Groceries", parent2Result.Value.Id);

        ItShouldSucceedCategoryResult(result);
        ItShouldHaveCategoryName(result, "Groceries");
    }

    [Fact]
    public void GivenParentId_WhenAddingCategory()
    {
        Menlo.Lib.Budget.Entities.Budget budget = CreateDraftBudget();
        Result<CategoryNode, BudgetError> parentResult = budget.AddCategory("Food");
        BudgetCategoryId parentId = parentResult.Value.Id;

        Result<CategoryNode, BudgetError> result = budget.AddCategory("Groceries", parentId);

        ItShouldSucceedCategoryResult(result);
        ItShouldHaveParentId(result, parentId);
    }

    [Fact]
    public void GivenDeepNesting_WhenAddingCategory()
    {
        Menlo.Lib.Budget.Entities.Budget budget = CreateDraftBudget();
        Result<CategoryNode, BudgetError> current = budget.AddCategory("Level1");

        for (int i = 2; i <= 10; i++)
        {
            current = budget.AddCategory($"Level{i}", current.Value.Id);
            ItShouldSucceedCategoryResult(current);
        }

        budget.Categories.Count.ShouldBe(10);
        current.Value.Name.Value.ShouldBe("Level10");
    }

    [Fact]
    public void GivenValidCategory_WhenAddingCategory_DefaultPlannedAmountIsZeroZar()
    {
        Menlo.Lib.Budget.Entities.Budget budget = CreateDraftBudget();

        Result<CategoryNode, BudgetError> result = budget.AddCategory("Groceries");

        ItShouldSucceedCategoryResult(result);
        result.Value.PlannedMonthlyAmount.Amount.ShouldBe(0m);
        result.Value.PlannedMonthlyAmount.Currency.ShouldBe("ZAR");
    }

    // -------------------------------------------------------------------------
    // Budget.SetPlanned
    // -------------------------------------------------------------------------

    [Fact]
    public void GivenValidAmountAndCategoryId_WhenSettingPlanned()
    {
        Menlo.Lib.Budget.Entities.Budget budget = CreateDraftBudget();
        CategoryNode category = budget.AddCategory("Groceries").Value;
        Money amount = Money.Create(500m, "ZAR").Value;

        UnitResult<BudgetError> result = budget.SetPlanned(category.Id, amount);

        result.IsSuccess.ShouldBeTrue();
        category.PlannedMonthlyAmount.Amount.ShouldBe(500m);
        category.PlannedMonthlyAmount.Currency.ShouldBe("ZAR");
    }

    [Fact]
    public void GivenNegativeAmount_WhenSettingPlanned()
    {
        Menlo.Lib.Budget.Entities.Budget budget = CreateDraftBudget();
        CategoryNode category = budget.AddCategory("Groceries").Value;
        Money negativeAmount = Money.Create(-100m, "ZAR").Value;

        UnitResult<BudgetError> result = budget.SetPlanned(category.Id, negativeAmount);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<InvalidBudgetDataError>();
    }

    [Fact]
    public void GivenUnknownCategoryId_WhenSettingPlanned()
    {
        Menlo.Lib.Budget.Entities.Budget budget = CreateDraftBudget();
        BudgetCategoryId unknownId = BudgetCategoryId.NewId();
        Money amount = Money.Create(100m, "ZAR").Value;

        UnitResult<BudgetError> result = budget.SetPlanned(unknownId, amount);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<InvalidBudgetDataError>();
    }

    [Fact]
    public void GivenZeroAmount_WhenSettingPlanned()
    {
        Menlo.Lib.Budget.Entities.Budget budget = CreateDraftBudget();
        CategoryNode category = budget.AddCategory("Groceries").Value;
        Money zeroAmount = Money.Zero("ZAR");

        UnitResult<BudgetError> result = budget.SetPlanned(category.Id, zeroAmount);

        result.IsSuccess.ShouldBeTrue();
        category.PlannedMonthlyAmount.Amount.ShouldBe(0m);
    }

    [Fact]
    public void GivenValidAmountAndCategory_WhenSettingPlanned_RaisesPlannedAmountSetEvent()
    {
        Menlo.Lib.Budget.Entities.Budget budget = CreateDraftBudget();
        CategoryNode category = budget.AddCategory("Groceries").Value;
        budget.ClearDomainEvents();
        Money amount = Money.Create(750m, "ZAR").Value;

        budget.SetPlanned(category.Id, amount);

        budget.DomainEvents.ShouldContain(e => e is PlannedAmountSetEvent);
        PlannedAmountSetEvent evt = budget.DomainEvents.OfType<PlannedAmountSetEvent>().Single();
        evt.BudgetId.ShouldBe(budget.Id);
        evt.CategoryId.ShouldBe(category.Id);
        evt.Amount.Amount.ShouldBe(750m);
    }

    // -------------------------------------------------------------------------
    // Budget.Activate
    // -------------------------------------------------------------------------

    [Fact]
    public void GivenDraftBudgetWithNonZeroCategory_WhenActivating()
    {
        Menlo.Lib.Budget.Entities.Budget budget = CreateDraftBudget();
        CategoryNode category = budget.AddCategory("Groceries").Value;
        budget.SetPlanned(category.Id, Money.Create(500m, "ZAR").Value);

        UnitResult<BudgetError> result = budget.Activate();

        result.IsSuccess.ShouldBeTrue();
        budget.Status.ShouldBe(BudgetStatus.Active);
    }

    [Fact]
    public void GivenDraftBudgetWithNoNonZeroCategory_WhenActivating()
    {
        Menlo.Lib.Budget.Entities.Budget budget = CreateDraftBudget();
        budget.AddCategory("Groceries");

        UnitResult<BudgetError> result = budget.Activate();

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<BudgetActivationError>();
    }

    [Fact]
    public void GivenActiveBudget_WhenActivating()
    {
        Menlo.Lib.Budget.Entities.Budget budget = CreateActiveBudget();

        UnitResult<BudgetError> result = budget.Activate();

        result.IsFailure.ShouldBeTrue();
        ItShouldBeInvalidBudgetStatusErrorForActivate(result, "Active");
    }

    [Fact]
    public void GivenClosedBudget_WhenActivating()
    {
        Menlo.Lib.Budget.Entities.Budget budget = CreateClosedBudget();

        UnitResult<BudgetError> result = budget.Activate();

        result.IsFailure.ShouldBeTrue();
        ItShouldBeInvalidBudgetStatusErrorForActivate(result, "Closed");
    }

    // -------------------------------------------------------------------------
    // Budget.Close
    // -------------------------------------------------------------------------

    [Fact]
    public void GivenActiveBudget_WhenClosing()
    {
        Menlo.Lib.Budget.Entities.Budget budget = CreateActiveBudget();

        UnitResult<BudgetError> result = budget.Close();

        result.IsSuccess.ShouldBeTrue();
        budget.Status.ShouldBe(BudgetStatus.Closed);
    }

    [Fact]
    public void GivenDraftBudget_WhenClosing()
    {
        Menlo.Lib.Budget.Entities.Budget budget = CreateDraftBudget();

        UnitResult<BudgetError> result = budget.Close();

        result.IsFailure.ShouldBeTrue();
        ItShouldBeInvalidBudgetStatusErrorForClose(result, "Draft");
    }

    [Fact]
    public void GivenAlreadyClosedBudget_WhenClosing()
    {
        Menlo.Lib.Budget.Entities.Budget budget = CreateClosedBudget();

        UnitResult<BudgetError> result = budget.Close();

        result.IsFailure.ShouldBeTrue();
        ItShouldBeInvalidBudgetStatusErrorForClose(result, "Closed");
    }

    // -------------------------------------------------------------------------
    // ISoftDeletable
    // -------------------------------------------------------------------------

    [Fact]
    public void GivenValidBudget_WhenDeleting()
    {
        Menlo.Lib.Budget.Entities.Budget budget = CreateDraftBudget();
        ISoftDeleteStampFactory factory = CreateSoftDeleteStampFactory();

        budget.Delete(factory);

        budget.IsDeleted.ShouldBeTrue();
        budget.DeletedAt.ShouldNotBeNull();
        budget.DeletedBy.ShouldNotBeNull();
    }

    // -------------------------------------------------------------------------
    // TotalPlannedMonthlyAmount
    // -------------------------------------------------------------------------

    [Fact]
    public void GivenMultipleCategories_TotalPlannedMonthlyAmountIsSumOfAll()
    {
        Menlo.Lib.Budget.Entities.Budget budget = CreateDraftBudget();
        CategoryNode cat1 = budget.AddCategory("Groceries").Value;
        CategoryNode cat2 = budget.AddCategory("Transport").Value;
        CategoryNode cat3 = budget.AddCategory("Fuel", cat2.Id).Value;

        budget.SetPlanned(cat1.Id, Money.Create(1000m, "ZAR").Value);
        budget.SetPlanned(cat2.Id, Money.Create(500m, "ZAR").Value);
        budget.SetPlanned(cat3.Id, Money.Create(300m, "ZAR").Value);

        budget.TotalPlannedMonthlyAmount.Amount.ShouldBe(1800m);
        budget.TotalPlannedMonthlyAmount.Currency.ShouldBe("ZAR");
    }

    // -------------------------------------------------------------------------
    // CloneForYear
    // -------------------------------------------------------------------------

    [Fact]
    public void GivenSourceBudget_WhenCloningForYear()
    {
        Menlo.Lib.Budget.Entities.Budget source = CreateDraftBudget(year: 2024);
        CategoryNode cat1 = source.AddCategory("Groceries").Value;
        CategoryNode cat2 = source.AddCategory("Transport").Value;
        source.SetPlanned(cat1.Id, Money.Create(1000m, "ZAR").Value);
        source.SetPlanned(cat2.Id, Money.Create(500m, "ZAR").Value);
        IAuditStampFactory factory = CreateAuditStampFactory();

        Result<Menlo.Lib.Budget.Entities.Budget, BudgetError> result =
            Menlo.Lib.Budget.Entities.Budget.CloneForYear(source, 2025, factory);

        ItShouldSucceed(result);
        ItShouldHaveYear(result, 2025);
        ItShouldHaveHouseholdId(result, source.HouseholdId);
        ItShouldHaveStatus(result, BudgetStatus.Draft);
        ItShouldHaveClonedCategories(result, source);
    }

    [Fact]
    public void GivenSourceBudgetWithNestedCategories_WhenCloningForYear_ThenParentChildRelationshipsPreserved()
    {
        Menlo.Lib.Budget.Entities.Budget source = CreateDraftBudget(year: 2024);
        CategoryNode parent = source.AddCategory("Living Expenses").Value;
        CategoryNode child = source.AddCategory("Rent", parent.Id).Value;
        CategoryNode grandchild = source.AddCategory("Maintenance", child.Id).Value;
        source.SetPlanned(parent.Id, Money.Create(5000m, "ZAR").Value);
        source.SetPlanned(child.Id, Money.Create(3000m, "ZAR").Value);
        source.SetPlanned(grandchild.Id, Money.Create(500m, "ZAR").Value);
        IAuditStampFactory factory = CreateAuditStampFactory();

        Result<Menlo.Lib.Budget.Entities.Budget, BudgetError> result =
            Menlo.Lib.Budget.Entities.Budget.CloneForYear(source, 2025, factory);

        ItShouldSucceed(result);
        ItShouldHaveClonedCategoriesWithParentChildRelationships(result, source);
    }

    [Fact]
    public void GivenClone_WhenModifyingSource_ThenCloneIsNotAffected()
    {
        Menlo.Lib.Budget.Entities.Budget source = CreateDraftBudget(year: 2024);
        CategoryNode cat = source.AddCategory("Groceries").Value;
        source.SetPlanned(cat.Id, Money.Create(1000m, "ZAR").Value);
        IAuditStampFactory factory = CreateAuditStampFactory();

        Result<Menlo.Lib.Budget.Entities.Budget, BudgetError> result =
            Menlo.Lib.Budget.Entities.Budget.CloneForYear(source, 2025, factory);

        // Modify source after cloning
        source.AddCategory("NewCategory");

        // Clone should not be affected
        result.Value.Categories.Count.ShouldBe(1);
    }

    // =========================================================================
    // Assertion helpers — Budget.Create / general
    // =========================================================================

    private static void ItShouldSucceed(Result<Menlo.Lib.Budget.Entities.Budget, BudgetError> result)
        => result.IsSuccess.ShouldBeTrue();

    private static void ItShouldFail(Result<Menlo.Lib.Budget.Entities.Budget, BudgetError> result)
        => result.IsFailure.ShouldBeTrue();

    private static void ItShouldHaveHouseholdId(
        Result<Menlo.Lib.Budget.Entities.Budget, BudgetError> result,
        HouseholdId expectedHouseholdId)
        => result.Value.HouseholdId.ShouldBe(expectedHouseholdId);

    private static void ItShouldHaveYear(
        Result<Menlo.Lib.Budget.Entities.Budget, BudgetError> result,
        int expectedYear)
        => result.Value.Year.ShouldBe(expectedYear);

    private static void ItShouldHaveEmptyCategories(
        Result<Menlo.Lib.Budget.Entities.Budget, BudgetError> result)
        => result.Value.Categories.ShouldBeEmpty();

    private static void ItShouldHaveAuditStampsSet(
        Result<Menlo.Lib.Budget.Entities.Budget, BudgetError> result)
    {
        Menlo.Lib.Budget.Entities.Budget budget = result.Value;
        budget.CreatedBy.ShouldNotBeNull();
        budget.CreatedAt.ShouldNotBeNull();
        budget.ModifiedBy.ShouldNotBeNull();
        budget.ModifiedAt.ShouldNotBeNull();
    }

    private static void ItShouldRaiseBudgetCreatedEvent(
        Result<Menlo.Lib.Budget.Entities.Budget, BudgetError> result,
        int expectedYear)
    {
        Menlo.Lib.Budget.Entities.Budget budget = result.Value;
        budget.DomainEvents.Count.ShouldBe(1);
        IDomainEvent domainEvent = budget.DomainEvents.First();
        domainEvent.ShouldBeOfType<BudgetCreatedEvent>();
        BudgetCreatedEvent createdEvent = (BudgetCreatedEvent)domainEvent;
        createdEvent.BudgetId.ShouldBe(budget.Id);
        createdEvent.Year.ShouldBe(expectedYear);
    }

    private static void ItShouldHaveStatus(
        Result<Menlo.Lib.Budget.Entities.Budget, BudgetError> result,
        BudgetStatus expectedStatus)
        => result.Value.Status.ShouldBe(expectedStatus);

    private static void ItShouldHaveTotalPlannedMonthlyAmount(
        Result<Menlo.Lib.Budget.Entities.Budget, BudgetError> result,
        decimal expectedAmount,
        string expectedCurrency)
    {
        result.Value.TotalPlannedMonthlyAmount.Amount.ShouldBe(expectedAmount);
        result.Value.TotalPlannedMonthlyAmount.Currency.ShouldBe(expectedCurrency);
    }

    private static void ItShouldBeInvalidBudgetDataError(
        Result<Menlo.Lib.Budget.Entities.Budget, BudgetError> result)
        => result.Error.ShouldBeOfType<InvalidBudgetDataError>();

    // =========================================================================
    // Assertion helpers — AddCategory
    // =========================================================================

    private static void ItShouldSucceedCategoryResult(Result<CategoryNode, BudgetError> result)
        => result.IsSuccess.ShouldBeTrue();

    private static void ItShouldFailCategoryResult(Result<CategoryNode, BudgetError> result)
        => result.IsFailure.ShouldBeTrue();

    private static void ItShouldHaveCategoryName(Result<CategoryNode, BudgetError> result, string expectedName)
        => result.Value.Name.Value.ShouldBe(expectedName);

    private static void ItShouldHaveValidCategoryId(Result<CategoryNode, BudgetError> result)
        => result.Value.Id.Value.ShouldNotBe(Guid.Empty);

    private static void ItShouldHaveParentId(Result<CategoryNode, BudgetError> result, BudgetCategoryId expectedParentId)
        => result.Value.ParentId.ShouldBe(expectedParentId);

    private static void ItShouldRaiseBudgetCategoryAddedEvent(
        Menlo.Lib.Budget.Entities.Budget budget,
        CategoryNode category,
        BudgetCategoryId? expectedParentId)
    {
        IDomainEvent domainEvent = budget.DomainEvents.Last();
        domainEvent.ShouldBeOfType<BudgetCategoryAddedEvent>();
        BudgetCategoryAddedEvent addedEvent = (BudgetCategoryAddedEvent)domainEvent;
        addedEvent.BudgetId.ShouldBe(budget.Id);
        addedEvent.CategoryId.ShouldBe(category.Id);
        addedEvent.Name.ShouldBe(category.Name.Value);
        addedEvent.ParentId.ShouldBe(expectedParentId);
    }

    private static void ItShouldBeCategoryInvalidBudgetDataError(Result<CategoryNode, BudgetError> result)
        => result.Error.ShouldBeOfType<InvalidBudgetDataError>();

    private static void ItShouldBeDuplicateCategoryNameError(Result<CategoryNode, BudgetError> result, string name)
    {
        result.Error.ShouldBeOfType<DuplicateCategoryNameError>();
        DuplicateCategoryNameError error = (DuplicateCategoryNameError)result.Error;
        error.Name.ShouldBe(name);
    }

    // =========================================================================
    // Assertion helpers — Activate / Close
    // =========================================================================

    private static void ItShouldBeInvalidBudgetStatusErrorForActivate(
        UnitResult<BudgetError> result,
        string expectedCurrentStatus)
    {
        result.Error.ShouldBeOfType<InvalidBudgetStatusError>();
        InvalidBudgetStatusError error = (InvalidBudgetStatusError)result.Error;
        error.Operation.ShouldBe("Activate");
        error.CurrentStatus.ShouldBe(expectedCurrentStatus);
    }

    private static void ItShouldBeInvalidBudgetStatusErrorForClose(
        UnitResult<BudgetError> result,
        string expectedCurrentStatus)
    {
        result.Error.ShouldBeOfType<InvalidBudgetStatusError>();
        InvalidBudgetStatusError error = (InvalidBudgetStatusError)result.Error;
        error.Operation.ShouldBe("Close");
        error.CurrentStatus.ShouldBe(expectedCurrentStatus);
    }

    // =========================================================================
    // Assertion helpers — CloneForYear
    // =========================================================================

    private static void ItShouldHaveClonedCategories(
        Result<Menlo.Lib.Budget.Entities.Budget, BudgetError> result,
        Menlo.Lib.Budget.Entities.Budget source)
    {
        Menlo.Lib.Budget.Entities.Budget cloned = result.Value;
        cloned.Categories.Count.ShouldBe(source.Categories.Count);

        foreach (CategoryNode sourceCategory in source.Categories)
        {
            CategoryNode? match = cloned.Categories
                .FirstOrDefault(c => c.Name.Value == sourceCategory.Name.Value);
            match.ShouldNotBeNull();
            match.PlannedMonthlyAmount.Amount.ShouldBe(sourceCategory.PlannedMonthlyAmount.Amount);
            match.PlannedMonthlyAmount.Currency.ShouldBe(sourceCategory.PlannedMonthlyAmount.Currency);
        }
    }

    private static void ItShouldHaveClonedCategoriesWithParentChildRelationships(
        Result<Menlo.Lib.Budget.Entities.Budget, BudgetError> result,
        Menlo.Lib.Budget.Entities.Budget source)
    {
        Menlo.Lib.Budget.Entities.Budget cloned = result.Value;
        cloned.Categories.Count.ShouldBe(source.Categories.Count);

        // Build name->cloned node lookup
        Dictionary<string, CategoryNode> clonedByName = cloned.Categories
            .ToDictionary(c => c.Name.Value);

        foreach (CategoryNode sourceNode in source.Categories)
        {
            CategoryNode clonedNode = clonedByName[sourceNode.Name.Value];

            // IDs must differ
            clonedNode.Id.ShouldNotBe(sourceNode.Id);

            if (sourceNode.ParentId is null)
            {
                clonedNode.ParentId.ShouldBeNull();
            }
            else
            {
                // Find the source parent's name and check the cloned parent ID matches
                string parentName = source.Categories.First(c => c.Id == sourceNode.ParentId.Value).Name.Value;
                CategoryNode clonedParent = clonedByName[parentName];
                clonedNode.ParentId.ShouldBe(clonedParent.Id);
            }
        }
    }

    // =========================================================================
    // Test setup helpers
    // =========================================================================

    private static Menlo.Lib.Budget.Entities.Budget CreateDraftBudget(int year = 2025)
    {
        HouseholdId householdId = HouseholdId.NewId();
        IAuditStampFactory factory = CreateAuditStampFactory();
        return Menlo.Lib.Budget.Entities.Budget.Create(householdId, year, factory).Value;
    }

    private static Menlo.Lib.Budget.Entities.Budget CreateActiveBudget()
    {
        Menlo.Lib.Budget.Entities.Budget budget = CreateDraftBudget();
        CategoryNode category = budget.AddCategory("Groceries").Value;
        budget.SetPlanned(category.Id, Money.Create(500m, "ZAR").Value);
        budget.Activate();
        return budget;
    }

    private static Menlo.Lib.Budget.Entities.Budget CreateClosedBudget()
    {
        Menlo.Lib.Budget.Entities.Budget budget = CreateActiveBudget();
        budget.Close();
        return budget;
    }

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

    private sealed class FakeSoftDeleteStampFactory(SoftDeleteStamp stamp) : ISoftDeleteStampFactory
    {
        private readonly SoftDeleteStamp _stamp = stamp;
        public SoftDeleteStamp CreateStamp() => _stamp;
    }

    private static ISoftDeleteStampFactory CreateSoftDeleteStampFactory()
    {
        UserId actorId = UserId.NewId();
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;
        return new FakeSoftDeleteStampFactory(new SoftDeleteStamp(actorId, timestamp));
    }
}
