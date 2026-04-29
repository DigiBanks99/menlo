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

        Result<CategoryNode, BudgetError> result = budget.AddCategory("Groceries", BudgetFlow.Expense);

        ItShouldSucceedCategoryResult(result);
        ItShouldHaveCategoryName(result, "Groceries");
        ItShouldHaveValidCategoryId(result);
        ItShouldRaiseBudgetCategoryAddedEvent(budget, result.Value, null);
    }

    [Fact]
    public void GivenNameWithWhitespace_WhenAddingCategory()
    {
        Menlo.Lib.Budget.Entities.Budget budget = CreateDraftBudget();

        Result<CategoryNode, BudgetError> result = budget.AddCategory("  Groceries  ", BudgetFlow.Expense);

        ItShouldSucceedCategoryResult(result);
        ItShouldHaveCategoryName(result, "Groceries");
    }

    [Fact]
    public void GivenEmptyName_WhenAddingCategory()
    {
        Menlo.Lib.Budget.Entities.Budget budget = CreateDraftBudget();

        Result<CategoryNode, BudgetError> result = budget.AddCategory("", BudgetFlow.Expense);

        ItShouldFailCategoryResult(result);
        ItShouldBeCategoryInvalidBudgetDataError(result);
    }

    [Fact]
    public void GivenDuplicateSiblingName_WhenAddingCategory()
    {
        Menlo.Lib.Budget.Entities.Budget budget = CreateDraftBudget();
        budget.AddCategory("Groceries", BudgetFlow.Expense);

        Result<CategoryNode, BudgetError> result = budget.AddCategory("GROCERIES", BudgetFlow.Expense);

        ItShouldFailCategoryResult(result);
        ItShouldBeDuplicateCategoryNameError(result, "GROCERIES");
    }

    [Fact]
    public void GivenDuplicateNameUnderDifferentParent_WhenAddingCategory()
    {
        Menlo.Lib.Budget.Entities.Budget budget = CreateDraftBudget();
        Result<CategoryNode, BudgetError> parent1Result = budget.AddCategory("Food", BudgetFlow.Expense);
        Result<CategoryNode, BudgetError> parent2Result = budget.AddCategory("Transport", BudgetFlow.Expense);

        budget.AddCategory("Groceries", BudgetFlow.Expense, parent1Result.Value.Id);
        Result<CategoryNode, BudgetError> result = budget.AddCategory("Groceries", BudgetFlow.Expense, parent2Result.Value.Id);

        ItShouldSucceedCategoryResult(result);
        ItShouldHaveCategoryName(result, "Groceries");
    }

    [Fact]
    public void GivenParentId_WhenAddingCategory()
    {
        Menlo.Lib.Budget.Entities.Budget budget = CreateDraftBudget();
        Result<CategoryNode, BudgetError> parentResult = budget.AddCategory("Food", BudgetFlow.Expense);
        BudgetCategoryId parentId = parentResult.Value.Id;

        Result<CategoryNode, BudgetError> result = budget.AddCategory("Groceries", BudgetFlow.Expense, parentId);

        ItShouldSucceedCategoryResult(result);
        ItShouldHaveParentId(result, parentId);
    }

    [Fact]
    public void GivenChildCategory_WhenAddingGrandchild_ThenDepthErrorReturned()
    {
        Menlo.Lib.Budget.Entities.Budget budget = CreateDraftBudget();
        CategoryNode root = budget.AddCategory("Level1", BudgetFlow.Expense).Value;
        CategoryNode child = budget.AddCategory("Level2", BudgetFlow.Expense, root.Id).Value;

        Result<CategoryNode, BudgetError> result = budget.AddCategory("Level3", BudgetFlow.Expense, child.Id);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<CategoryDepthError>();
    }

    [Fact]
    public void GivenValidCategory_WhenAddingCategory_DefaultPlannedAmountIsZeroZar()
    {
        Menlo.Lib.Budget.Entities.Budget budget = CreateDraftBudget();

        Result<CategoryNode, BudgetError> result = budget.AddCategory("Groceries", BudgetFlow.Expense);

        ItShouldSucceedCategoryResult(result);
        result.Value.PlannedMonthlyAmount.Amount.ShouldBe(0m);
        result.Value.PlannedMonthlyAmount.Currency.ShouldBe("ZAR");
    }

    // -------------------------------------------------------------------------
    // Budget.Activate
    // -------------------------------------------------------------------------

    [Fact]
    public void GivenDraftBudgetWithCategory_WhenActivating()
    {
        Menlo.Lib.Budget.Entities.Budget budget = CreateDraftBudget();
        budget.AddCategory("Groceries", BudgetFlow.Expense);

        UnitResult<BudgetError> result = budget.Activate();

        result.IsSuccess.ShouldBeTrue();
        budget.Status.ShouldBe(BudgetStatus.Active);
    }

    [Fact]
    public void GivenDraftBudgetWithNoCategories_WhenActivating()
    {
        Menlo.Lib.Budget.Entities.Budget budget = CreateDraftBudget();

        UnitResult<BudgetError> result = budget.Activate();

        result.IsSuccess.ShouldBeTrue();
        budget.Status.ShouldBe(BudgetStatus.Active);
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
    public void GivenMultipleCategories_TotalPlannedMonthlyAmountIsAlwaysZero()
    {
        Menlo.Lib.Budget.Entities.Budget budget = CreateDraftBudget();
        budget.AddCategory("Groceries", BudgetFlow.Expense);
        budget.AddCategory("Transport", BudgetFlow.Expense);

        budget.TotalPlannedMonthlyAmount.Amount.ShouldBe(0m);
        budget.TotalPlannedMonthlyAmount.Currency.ShouldBe("ZAR");
    }

    // -------------------------------------------------------------------------
    // CloneForYear
    // -------------------------------------------------------------------------

    [Fact]
    public void GivenSourceBudget_WhenCloningForYear()
    {
        Menlo.Lib.Budget.Entities.Budget source = CreateDraftBudget(year: 2024);
        source.AddCategory("Groceries", BudgetFlow.Expense);
        source.AddCategory("Transport", BudgetFlow.Expense);
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
        CategoryNode parent = source.AddCategory("Living Expenses", BudgetFlow.Expense).Value;
        source.AddCategory("Rent", BudgetFlow.Expense, parent.Id);
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
        source.AddCategory("Groceries", BudgetFlow.Expense);
        IAuditStampFactory factory = CreateAuditStampFactory();

        Result<Menlo.Lib.Budget.Entities.Budget, BudgetError> result =
            Menlo.Lib.Budget.Entities.Budget.CloneForYear(source, 2025, factory);

        // Modify source after cloning
        source.AddCategory("NewCategory", BudgetFlow.Expense);

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
        budget.AddCategory("Groceries", BudgetFlow.Expense);
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

    // -------------------------------------------------------------------------
    // Budget.AddCategory — extended tests
    // -------------------------------------------------------------------------

    [Fact]
    public void GivenAllOptionalFields_WhenAddingCategory()
    {
        Menlo.Lib.Budget.Entities.Budget budget = CreateDraftBudget();

        Result<CategoryNode, BudgetError> result = budget.AddCategory(
            "Salary",
            BudgetFlow.Income,
            description: "Monthly salary",
            attribution: Attribution.Main,
            incomeContributor: "John",
            responsiblePayer: "Employer");

        ItShouldSucceedCategoryResult(result);
        ItShouldHaveCategoryName(result, "Salary");
        ItShouldHaveBudgetFlow(result, BudgetFlow.Income);
        ItShouldHaveAttribution(result, Attribution.Main);
        ItShouldHaveDescription(result, "Monthly salary");
        ItShouldHaveIncomeContributor(result, "John");
        ItShouldHaveResponsiblePayer(result, "Employer");
        ItShouldHaveCanonicalCategoryId(result);
    }

    [Fact]
    public void GivenNonExistentParent_WhenAddingCategory()
    {
        Menlo.Lib.Budget.Entities.Budget budget = CreateDraftBudget();
        BudgetCategoryId fakeParentId = BudgetCategoryId.NewId();

        Result<CategoryNode, BudgetError> result = budget.AddCategory("Child", BudgetFlow.Expense, fakeParentId);

        ItShouldFailCategoryResult(result);
        result.Error.ShouldBeOfType<CategoryNotFoundError>();
    }

    [Fact]
    public void GivenSoftDeletedParent_WhenAddingCategory()
    {
        Menlo.Lib.Budget.Entities.Budget budget = CreateDraftBudget();
        CategoryNode parent = budget.AddCategory("Parent", BudgetFlow.Expense).Value;
        budget.SoftDeleteCategory(parent.Id, CreateSoftDeleteStampFactory());

        Result<CategoryNode, BudgetError> result = budget.AddCategory("Child", BudgetFlow.Expense, parent.Id);

        ItShouldFailCategoryResult(result);
        result.Error.ShouldBeOfType<DeletedParentError>();
    }

    [Fact]
    public void GivenDuplicateNameButExistingSiblingIsDeleted_WhenAddingCategory()
    {
        Menlo.Lib.Budget.Entities.Budget budget = CreateDraftBudget();
        CategoryNode original = budget.AddCategory("Groceries", BudgetFlow.Expense).Value;
        budget.SoftDeleteCategory(original.Id, CreateSoftDeleteStampFactory());

        Result<CategoryNode, BudgetError> result = budget.AddCategory("Groceries", BudgetFlow.Expense);

        ItShouldSucceedCategoryResult(result);
        ItShouldHaveCategoryName(result, "Groceries");
    }

    [Fact]
    public void GivenWhitespaceOnlyName_WhenAddingCategory()
    {
        Menlo.Lib.Budget.Entities.Budget budget = CreateDraftBudget();

        Result<CategoryNode, BudgetError> result = budget.AddCategory("   ", BudgetFlow.Expense);

        ItShouldFailCategoryResult(result);
        ItShouldBeCategoryInvalidBudgetDataError(result);
    }

    [Fact]
    public void GivenBudgetFlowIncome_WhenAddingCategory_ThenBudgetFlowIsIncome()
    {
        Menlo.Lib.Budget.Entities.Budget budget = CreateDraftBudget();

        Result<CategoryNode, BudgetError> result = budget.AddCategory("Salary", BudgetFlow.Income);

        ItShouldSucceedCategoryResult(result);
        ItShouldHaveBudgetFlow(result, BudgetFlow.Income);
    }

    [Fact]
    public void GivenBudgetFlowBoth_WhenAddingCategory_ThenBudgetFlowIsBoth()
    {
        Menlo.Lib.Budget.Entities.Budget budget = CreateDraftBudget();

        Result<CategoryNode, BudgetError> result = budget.AddCategory("Mixed", BudgetFlow.Both);

        ItShouldSucceedCategoryResult(result);
        ItShouldHaveBudgetFlow(result, BudgetFlow.Both);
    }

    [Fact]
    public void GivenSubcategory_WhenAddingCategory_ThenRaisesEventWithParentId()
    {
        Menlo.Lib.Budget.Entities.Budget budget = CreateDraftBudget();
        CategoryNode parent = budget.AddCategory("Food", BudgetFlow.Expense).Value;

        Result<CategoryNode, BudgetError> result = budget.AddCategory("Groceries", BudgetFlow.Expense, parent.Id);

        ItShouldSucceedCategoryResult(result);
        ItShouldRaiseBudgetCategoryAddedEvent(budget, result.Value, parent.Id);
    }

    // -------------------------------------------------------------------------
    // Budget.UpdateCategory
    // -------------------------------------------------------------------------

    [Fact]
    public void GivenValidCategory_WhenUpdatingName()
    {
        Menlo.Lib.Budget.Entities.Budget budget = CreateDraftBudget();
        CategoryNode category = budget.AddCategory("OldName", BudgetFlow.Expense).Value;

        Result<CategoryNode, BudgetError> result = budget.UpdateCategory(
            category.Id, "NewName", BudgetFlow.Expense);

        ItShouldSucceedCategoryResult(result);
        ItShouldHaveCategoryName(result, "NewName");
    }

    [Fact]
    public void GivenValidCategory_WhenUpdatingAllFields()
    {
        Menlo.Lib.Budget.Entities.Budget budget = CreateDraftBudget();
        CategoryNode category = budget.AddCategory("Salary", BudgetFlow.Income).Value;

        Result<CategoryNode, BudgetError> result = budget.UpdateCategory(
            category.Id,
            "Updated Salary",
            BudgetFlow.Both,
            attribution: Attribution.Rental,
            description: "Updated description",
            incomeContributor: "Jane",
            responsiblePayer: "Corp");

        ItShouldSucceedCategoryResult(result);
        ItShouldHaveCategoryName(result, "Updated Salary");
        ItShouldHaveBudgetFlow(result, BudgetFlow.Both);
        ItShouldHaveAttribution(result, Attribution.Rental);
        ItShouldHaveDescription(result, "Updated description");
        ItShouldHaveIncomeContributor(result, "Jane");
        ItShouldHaveResponsiblePayer(result, "Corp");
    }

    [Fact]
    public void GivenDuplicateSiblingName_WhenUpdatingCategory()
    {
        Menlo.Lib.Budget.Entities.Budget budget = CreateDraftBudget();
        budget.AddCategory("Existing", BudgetFlow.Expense);
        CategoryNode target = budget.AddCategory("Target", BudgetFlow.Expense).Value;

        Result<CategoryNode, BudgetError> result = budget.UpdateCategory(
            target.Id, "Existing", BudgetFlow.Expense);

        ItShouldFailCategoryResult(result);
        ItShouldBeDuplicateCategoryNameError(result, "Existing");
    }

    [Fact]
    public void GivenNonExistentCategory_WhenUpdating()
    {
        Menlo.Lib.Budget.Entities.Budget budget = CreateDraftBudget();
        BudgetCategoryId fakeId = BudgetCategoryId.NewId();

        Result<CategoryNode, BudgetError> result = budget.UpdateCategory(
            fakeId, "NewName", BudgetFlow.Expense);

        ItShouldFailCategoryResult(result);
        result.Error.ShouldBeOfType<CategoryNotFoundError>();
    }

    [Fact]
    public void GivenSoftDeletedCategory_WhenUpdating()
    {
        Menlo.Lib.Budget.Entities.Budget budget = CreateDraftBudget();
        CategoryNode category = budget.AddCategory("ToDelete", BudgetFlow.Expense).Value;
        budget.SoftDeleteCategory(category.Id, CreateSoftDeleteStampFactory());

        Result<CategoryNode, BudgetError> result = budget.UpdateCategory(
            category.Id, "Updated", BudgetFlow.Expense);

        ItShouldFailCategoryResult(result);
        ItShouldBeCategoryInvalidBudgetDataError(result);
    }

    [Fact]
    public void GivenValidCategory_WhenUpdating_ThenRaisesCategoryUpdatedEvent()
    {
        Menlo.Lib.Budget.Entities.Budget budget = CreateDraftBudget();
        CategoryNode category = budget.AddCategory("Cat", BudgetFlow.Expense).Value;
        budget.ClearDomainEvents();

        budget.UpdateCategory(category.Id, "UpdatedCat", BudgetFlow.Expense);

        ItShouldRaiseCategoryUpdatedEvent(budget, category.Id);
    }

    [Fact]
    public void GivenValidCategory_WhenUpdating_ThenCanonicalCategoryIdIsPreserved()
    {
        Menlo.Lib.Budget.Entities.Budget budget = CreateDraftBudget();
        CategoryNode category = budget.AddCategory("Cat", BudgetFlow.Expense).Value;
        CanonicalCategoryId originalCanonicalId = category.CanonicalCategoryId;

        Result<CategoryNode, BudgetError> result = budget.UpdateCategory(
            category.Id, "UpdatedCat", BudgetFlow.Income);

        ItShouldSucceedCategoryResult(result);
        result.Value.CanonicalCategoryId.ShouldBe(originalCanonicalId);
    }

    [Fact]
    public void GivenSameNameAsSelf_WhenUpdatingCategory_ThenSucceeds()
    {
        Menlo.Lib.Budget.Entities.Budget budget = CreateDraftBudget();
        CategoryNode category = budget.AddCategory("SameName", BudgetFlow.Expense).Value;

        Result<CategoryNode, BudgetError> result = budget.UpdateCategory(
            category.Id, "SameName", BudgetFlow.Income);

        ItShouldSucceedCategoryResult(result);
        ItShouldHaveBudgetFlow(result, BudgetFlow.Income);
    }

    // -------------------------------------------------------------------------
    // Budget.ReparentCategory
    // -------------------------------------------------------------------------

    [Fact]
    public void GivenChildCategory_WhenReparentingToDifferentRoot()
    {
        Menlo.Lib.Budget.Entities.Budget budget = CreateDraftBudget();
        CategoryNode root1 = budget.AddCategory("Root1", BudgetFlow.Expense).Value;
        CategoryNode root2 = budget.AddCategory("Root2", BudgetFlow.Expense).Value;
        CategoryNode child = budget.AddCategory("Child", BudgetFlow.Expense, root1.Id).Value;
        budget.ClearDomainEvents();

        Result<CategoryNode, BudgetError> result = budget.ReparentCategory(child.Id, root2.Id);

        ItShouldSucceedCategoryResult(result);
        ItShouldHaveParentId(result, root2.Id);
        ItShouldRaiseCategoryReparentedEvent(budget, child.Id, root1.Id, root2.Id);
    }

    [Fact]
    public void GivenChildCategory_WhenPromotingToRoot()
    {
        Menlo.Lib.Budget.Entities.Budget budget = CreateDraftBudget();
        CategoryNode root = budget.AddCategory("Root", BudgetFlow.Expense).Value;
        CategoryNode child = budget.AddCategory("Child", BudgetFlow.Expense, root.Id).Value;
        budget.ClearDomainEvents();

        Result<CategoryNode, BudgetError> result = budget.ReparentCategory(child.Id, null);

        ItShouldSucceedCategoryResult(result);
        result.Value.ParentId.ShouldBeNull();
        ItShouldRaiseCategoryReparentedEvent(budget, child.Id, root.Id, null);
    }

    [Fact]
    public void GivenRootWithChildren_WhenReparentingUnderAnotherRoot_ThenDepthError()
    {
        Menlo.Lib.Budget.Entities.Budget budget = CreateDraftBudget();
        CategoryNode root1 = budget.AddCategory("Root1", BudgetFlow.Expense).Value;
        budget.AddCategory("Child1", BudgetFlow.Expense, root1.Id);
        CategoryNode root2 = budget.AddCategory("Root2", BudgetFlow.Expense).Value;

        Result<CategoryNode, BudgetError> result = budget.ReparentCategory(root1.Id, root2.Id);

        ItShouldFailCategoryResult(result);
        result.Error.ShouldBeOfType<CategoryDepthError>();
    }

    [Fact]
    public void GivenCategory_WhenReparentingUnderAChild_ThenDepthError()
    {
        Menlo.Lib.Budget.Entities.Budget budget = CreateDraftBudget();
        CategoryNode root = budget.AddCategory("Root", BudgetFlow.Expense).Value;
        CategoryNode child = budget.AddCategory("Child", BudgetFlow.Expense, root.Id).Value;
        CategoryNode orphan = budget.AddCategory("Orphan", BudgetFlow.Expense).Value;

        Result<CategoryNode, BudgetError> result = budget.ReparentCategory(orphan.Id, child.Id);

        ItShouldFailCategoryResult(result);
        result.Error.ShouldBeOfType<CategoryDepthError>();
    }

    [Fact]
    public void GivenCategory_WhenReparentingToSelf_ThenInvalidBudgetDataError()
    {
        Menlo.Lib.Budget.Entities.Budget budget = CreateDraftBudget();
        CategoryNode category = budget.AddCategory("Self", BudgetFlow.Expense).Value;

        Result<CategoryNode, BudgetError> result = budget.ReparentCategory(category.Id, category.Id);

        ItShouldFailCategoryResult(result);
        ItShouldBeCategoryInvalidBudgetDataError(result);
    }

    [Fact]
    public void GivenDeletedParent_WhenReparentingCategory_ThenDeletedParentError()
    {
        Menlo.Lib.Budget.Entities.Budget budget = CreateDraftBudget();
        CategoryNode root = budget.AddCategory("Root", BudgetFlow.Expense).Value;
        CategoryNode target = budget.AddCategory("Target", BudgetFlow.Expense).Value;
        budget.SoftDeleteCategory(root.Id, CreateSoftDeleteStampFactory());

        Result<CategoryNode, BudgetError> result = budget.ReparentCategory(target.Id, root.Id);

        ItShouldFailCategoryResult(result);
        result.Error.ShouldBeOfType<DeletedParentError>();
    }

    [Fact]
    public void GivenNonExistentNewParent_WhenReparentingCategory()
    {
        Menlo.Lib.Budget.Entities.Budget budget = CreateDraftBudget();
        CategoryNode category = budget.AddCategory("Cat", BudgetFlow.Expense).Value;
        BudgetCategoryId fakeId = BudgetCategoryId.NewId();

        Result<CategoryNode, BudgetError> result = budget.ReparentCategory(category.Id, fakeId);

        ItShouldFailCategoryResult(result);
        result.Error.ShouldBeOfType<CategoryNotFoundError>();
    }

    [Fact]
    public void GivenNonExistentCategory_WhenReparenting()
    {
        Menlo.Lib.Budget.Entities.Budget budget = CreateDraftBudget();
        CategoryNode root = budget.AddCategory("Root", BudgetFlow.Expense).Value;
        BudgetCategoryId fakeId = BudgetCategoryId.NewId();

        Result<CategoryNode, BudgetError> result = budget.ReparentCategory(fakeId, root.Id);

        ItShouldFailCategoryResult(result);
        result.Error.ShouldBeOfType<CategoryNotFoundError>();
    }

    [Fact]
    public void GivenSoftDeletedCategory_WhenReparenting()
    {
        Menlo.Lib.Budget.Entities.Budget budget = CreateDraftBudget();
        CategoryNode root = budget.AddCategory("Root", BudgetFlow.Expense).Value;
        CategoryNode target = budget.AddCategory("Target", BudgetFlow.Expense).Value;
        budget.SoftDeleteCategory(target.Id, CreateSoftDeleteStampFactory());

        Result<CategoryNode, BudgetError> result = budget.ReparentCategory(target.Id, root.Id);

        ItShouldFailCategoryResult(result);
        ItShouldBeCategoryInvalidBudgetDataError(result);
    }

    [Fact]
    public void GivenDuplicateNameInNewScope_WhenReparenting_ThenDuplicateNameError()
    {
        Menlo.Lib.Budget.Entities.Budget budget = CreateDraftBudget();
        CategoryNode root1 = budget.AddCategory("Root1", BudgetFlow.Expense).Value;
        CategoryNode root2 = budget.AddCategory("Root2", BudgetFlow.Expense).Value;
        budget.AddCategory("SameName", BudgetFlow.Expense, root1.Id);
        CategoryNode moveable = budget.AddCategory("SameName", BudgetFlow.Expense, root2.Id).Value;

        Result<CategoryNode, BudgetError> result = budget.ReparentCategory(moveable.Id, root1.Id);

        ItShouldFailCategoryResult(result);
        ItShouldBeDuplicateCategoryNameError(result, "SameName");
    }

    // -------------------------------------------------------------------------
    // Budget.SoftDeleteCategory
    // -------------------------------------------------------------------------

    [Fact]
    public void GivenLeafCategory_WhenSoftDeleting()
    {
        Menlo.Lib.Budget.Entities.Budget budget = CreateDraftBudget();
        CategoryNode leaf = budget.AddCategory("Leaf", BudgetFlow.Expense).Value;
        budget.ClearDomainEvents();

        UnitResult<BudgetError> result = budget.SoftDeleteCategory(leaf.Id, CreateSoftDeleteStampFactory());

        result.IsSuccess.ShouldBeTrue();
        ItShouldHaveDeletedCategory(budget, leaf.Id);
        ItShouldRaiseCategorySoftDeletedEvent(budget, leaf.Id);
    }

    [Fact]
    public void GivenParentCategory_WhenSoftDeleting_ThenCascadesToChildren()
    {
        Menlo.Lib.Budget.Entities.Budget budget = CreateDraftBudget();
        CategoryNode parent = budget.AddCategory("Parent", BudgetFlow.Expense).Value;
        CategoryNode child = budget.AddCategory("Child", BudgetFlow.Expense, parent.Id).Value;
        budget.ClearDomainEvents();

        UnitResult<BudgetError> result = budget.SoftDeleteCategory(parent.Id, CreateSoftDeleteStampFactory());

        result.IsSuccess.ShouldBeTrue();
        ItShouldHaveDeletedCategory(budget, parent.Id);
        ItShouldHaveDeletedCategory(budget, child.Id);
    }

    [Fact]
    public void GivenAlreadyDeletedCategory_WhenSoftDeleting_ThenNoOpSuccess()
    {
        Menlo.Lib.Budget.Entities.Budget budget = CreateDraftBudget();
        CategoryNode category = budget.AddCategory("Cat", BudgetFlow.Expense).Value;
        budget.SoftDeleteCategory(category.Id, CreateSoftDeleteStampFactory());
        budget.ClearDomainEvents();

        UnitResult<BudgetError> result = budget.SoftDeleteCategory(category.Id, CreateSoftDeleteStampFactory());

        result.IsSuccess.ShouldBeTrue();
        budget.DomainEvents.ShouldBeEmpty(); // No event for no-op
    }

    [Fact]
    public void GivenNonExistentCategory_WhenSoftDeleting()
    {
        Menlo.Lib.Budget.Entities.Budget budget = CreateDraftBudget();
        BudgetCategoryId fakeId = BudgetCategoryId.NewId();

        UnitResult<BudgetError> result = budget.SoftDeleteCategory(fakeId, CreateSoftDeleteStampFactory());

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<CategoryNotFoundError>();
    }

    // -------------------------------------------------------------------------
    // Budget.RestoreCategory
    // -------------------------------------------------------------------------

    [Fact]
    public void GivenDeletedCategory_WhenRestoring()
    {
        Menlo.Lib.Budget.Entities.Budget budget = CreateDraftBudget();
        CategoryNode category = budget.AddCategory("Cat", BudgetFlow.Expense).Value;
        budget.SoftDeleteCategory(category.Id, CreateSoftDeleteStampFactory());
        budget.ClearDomainEvents();

        UnitResult<BudgetError> result = budget.RestoreCategory(category.Id);

        result.IsSuccess.ShouldBeTrue();
        ItShouldHaveActiveCategory(budget, category.Id);
        ItShouldRaiseCategoryRestoredEvent(budget, category.Id);
    }

    [Fact]
    public void GivenDeletedParentWithChildren_WhenRestoring_ThenCascadesToChildren()
    {
        Menlo.Lib.Budget.Entities.Budget budget = CreateDraftBudget();
        CategoryNode parent = budget.AddCategory("Parent", BudgetFlow.Expense).Value;
        CategoryNode child = budget.AddCategory("Child", BudgetFlow.Expense, parent.Id).Value;
        budget.SoftDeleteCategory(parent.Id, CreateSoftDeleteStampFactory());
        budget.ClearDomainEvents();

        UnitResult<BudgetError> result = budget.RestoreCategory(parent.Id);

        result.IsSuccess.ShouldBeTrue();
        ItShouldHaveActiveCategory(budget, parent.Id);
        ItShouldHaveActiveCategory(budget, child.Id);
    }

    [Fact]
    public void GivenAlreadyActiveCategory_WhenRestoring_ThenNoOpSuccess()
    {
        Menlo.Lib.Budget.Entities.Budget budget = CreateDraftBudget();
        CategoryNode category = budget.AddCategory("Cat", BudgetFlow.Expense).Value;
        budget.ClearDomainEvents();

        UnitResult<BudgetError> result = budget.RestoreCategory(category.Id);

        result.IsSuccess.ShouldBeTrue();
        budget.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void GivenDeletedChild_WhenRestoringWhileParentIsDeleted_ThenInvalidBudgetDataError()
    {
        Menlo.Lib.Budget.Entities.Budget budget = CreateDraftBudget();
        CategoryNode parent = budget.AddCategory("Parent", BudgetFlow.Expense).Value;
        CategoryNode child = budget.AddCategory("Child", BudgetFlow.Expense, parent.Id).Value;
        budget.SoftDeleteCategory(parent.Id, CreateSoftDeleteStampFactory());

        // Try to restore just the child while parent is still deleted
        UnitResult<BudgetError> result = budget.RestoreCategory(child.Id);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<InvalidBudgetDataError>();
    }

    [Fact]
    public void GivenNonExistentCategory_WhenRestoring()
    {
        Menlo.Lib.Budget.Entities.Budget budget = CreateDraftBudget();
        BudgetCategoryId fakeId = BudgetCategoryId.NewId();

        UnitResult<BudgetError> result = budget.RestoreCategory(fakeId);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<CategoryNotFoundError>();
    }

    // -------------------------------------------------------------------------
    // Budget.CloneForYear — extended tests
    // -------------------------------------------------------------------------

    [Fact]
    public void GivenSourceWithDeletedCategories_WhenCloningForYear_ThenDeletedCategoriesAreNotCloned()
    {
        Menlo.Lib.Budget.Entities.Budget source = CreateDraftBudget(year: 2024);
        source.AddCategory("Active", BudgetFlow.Expense);
        CategoryNode deleted = source.AddCategory("Deleted", BudgetFlow.Expense).Value;
        source.SoftDeleteCategory(deleted.Id, CreateSoftDeleteStampFactory());
        IAuditStampFactory factory = CreateAuditStampFactory();

        Result<Menlo.Lib.Budget.Entities.Budget, BudgetError> result =
            Menlo.Lib.Budget.Entities.Budget.CloneForYear(source, 2025, factory);

        ItShouldSucceed(result);
        result.Value.Categories.Count.ShouldBe(1);
        result.Value.Categories.First().Name.Value.ShouldBe("Active");
    }

    [Fact]
    public void GivenSourceWithCategories_WhenCloningForYear_ThenCanonicalCategoryIdsArePreserved()
    {
        Menlo.Lib.Budget.Entities.Budget source = CreateDraftBudget(year: 2024);
        CategoryNode sourceCategory = source.AddCategory("Groceries", BudgetFlow.Expense).Value;
        IAuditStampFactory factory = CreateAuditStampFactory();

        Result<Menlo.Lib.Budget.Entities.Budget, BudgetError> result =
            Menlo.Lib.Budget.Entities.Budget.CloneForYear(source, 2025, factory);

        ItShouldSucceed(result);
        CategoryNode clonedCategory = result.Value.Categories.First();
        clonedCategory.CanonicalCategoryId.ShouldBe(sourceCategory.CanonicalCategoryId);
    }

    [Fact]
    public void GivenSourceWithAllFields_WhenCloningForYear_ThenAllFieldsArePreserved()
    {
        Menlo.Lib.Budget.Entities.Budget source = CreateDraftBudget(year: 2024);
        source.AddCategory(
            "Salary",
            BudgetFlow.Income,
            description: "My salary",
            attribution: Attribution.Main,
            incomeContributor: "John",
            responsiblePayer: "Corp");
        IAuditStampFactory factory = CreateAuditStampFactory();

        Result<Menlo.Lib.Budget.Entities.Budget, BudgetError> result =
            Menlo.Lib.Budget.Entities.Budget.CloneForYear(source, 2025, factory);

        ItShouldSucceed(result);
        CategoryNode cloned = result.Value.Categories.First();
        cloned.BudgetFlow.ShouldBe(BudgetFlow.Income);
        cloned.Attribution.ShouldBe(Attribution.Main);
        cloned.Description.ShouldBe("My salary");
        cloned.IncomeContributor.ShouldBe("John");
        cloned.ResponsiblePayer.ShouldBe("Corp");
    }

    // -------------------------------------------------------------------------
    // Budget.Activate — extended tests
    // -------------------------------------------------------------------------

    [Fact]
    public void GivenDraftBudgetWithNoAmounts_WhenActivating_ThenSucceeds()
    {
        Menlo.Lib.Budget.Entities.Budget budget = CreateDraftBudget();

        UnitResult<BudgetError> result = budget.Activate();

        result.IsSuccess.ShouldBeTrue();
        budget.Status.ShouldBe(BudgetStatus.Active);
    }

    // -------------------------------------------------------------------------
    // CategoryNode.PlannedMonthlyAmount
    // -------------------------------------------------------------------------

    [Fact]
    public void GivenCategoryNode_PlannedMonthlyAmountIsAlwaysZero()
    {
        Menlo.Lib.Budget.Entities.Budget budget = CreateDraftBudget();
        CategoryNode category = budget.AddCategory("Test", BudgetFlow.Expense).Value;

        category.PlannedMonthlyAmount.Amount.ShouldBe(0m);
        category.PlannedMonthlyAmount.Currency.ShouldBe("ZAR");
    }

    // =========================================================================
    // Assertion helpers — UpdateCategory
    // =========================================================================

    private static void ItShouldHaveBudgetFlow(Result<CategoryNode, BudgetError> result, BudgetFlow expected)
        => result.Value.BudgetFlow.ShouldBe(expected);

    private static void ItShouldHaveAttribution(Result<CategoryNode, BudgetError> result, Attribution expected)
        => result.Value.Attribution.ShouldBe(expected);

    private static void ItShouldHaveDescription(Result<CategoryNode, BudgetError> result, string expected)
        => result.Value.Description.ShouldBe(expected);

    private static void ItShouldHaveIncomeContributor(Result<CategoryNode, BudgetError> result, string expected)
        => result.Value.IncomeContributor.ShouldBe(expected);

    private static void ItShouldHaveResponsiblePayer(Result<CategoryNode, BudgetError> result, string expected)
        => result.Value.ResponsiblePayer.ShouldBe(expected);

    private static void ItShouldHaveCanonicalCategoryId(Result<CategoryNode, BudgetError> result)
        => result.Value.CanonicalCategoryId.Value.ShouldNotBe(Guid.Empty);

    private static void ItShouldRaiseCategoryUpdatedEvent(
        Menlo.Lib.Budget.Entities.Budget budget, BudgetCategoryId categoryId)
    {
        IDomainEvent domainEvent = budget.DomainEvents.Last();
        domainEvent.ShouldBeOfType<CategoryUpdatedEvent>();
        CategoryUpdatedEvent evt = (CategoryUpdatedEvent)domainEvent;
        evt.BudgetId.ShouldBe(budget.Id);
        evt.CategoryId.ShouldBe(categoryId);
    }

    // =========================================================================
    // Assertion helpers — ReparentCategory
    // =========================================================================

    private static void ItShouldRaiseCategoryReparentedEvent(
        Menlo.Lib.Budget.Entities.Budget budget,
        BudgetCategoryId categoryId,
        BudgetCategoryId? oldParentId,
        BudgetCategoryId? newParentId)
    {
        IDomainEvent domainEvent = budget.DomainEvents.Last();
        domainEvent.ShouldBeOfType<CategoryReparentedEvent>();
        CategoryReparentedEvent evt = (CategoryReparentedEvent)domainEvent;
        evt.BudgetId.ShouldBe(budget.Id);
        evt.CategoryId.ShouldBe(categoryId);
        evt.OldParentId.ShouldBe(oldParentId);
        evt.NewParentId.ShouldBe(newParentId);
    }

    // =========================================================================
    // Assertion helpers — SoftDeleteCategory
    // =========================================================================

    private static void ItShouldHaveDeletedCategory(
        Menlo.Lib.Budget.Entities.Budget budget, BudgetCategoryId categoryId)
    {
        CategoryNode node = budget.Categories.First(c => c.Id == categoryId);
        node.IsDeleted.ShouldBeTrue();
        node.DeletedAt.ShouldNotBeNull();
        node.DeletedBy.ShouldNotBeNull();
    }

    private static void ItShouldRaiseCategorySoftDeletedEvent(
        Menlo.Lib.Budget.Entities.Budget budget, BudgetCategoryId categoryId)
    {
        IDomainEvent domainEvent = budget.DomainEvents.Last();
        domainEvent.ShouldBeOfType<CategorySoftDeletedEvent>();
        CategorySoftDeletedEvent evt = (CategorySoftDeletedEvent)domainEvent;
        evt.BudgetId.ShouldBe(budget.Id);
        evt.CategoryId.ShouldBe(categoryId);
    }

    // =========================================================================
    // Assertion helpers — RestoreCategory
    // =========================================================================

    private static void ItShouldHaveActiveCategory(
        Menlo.Lib.Budget.Entities.Budget budget, BudgetCategoryId categoryId)
    {
        CategoryNode node = budget.Categories.First(c => c.Id == categoryId);
        node.IsDeleted.ShouldBeFalse();
        node.DeletedAt.ShouldBeNull();
        node.DeletedBy.ShouldBeNull();
    }

    private static void ItShouldRaiseCategoryRestoredEvent(
        Menlo.Lib.Budget.Entities.Budget budget, BudgetCategoryId categoryId)
    {
        IDomainEvent domainEvent = budget.DomainEvents.Last();
        domainEvent.ShouldBeOfType<CategoryRestoredEvent>();
        CategoryRestoredEvent evt = (CategoryRestoredEvent)domainEvent;
        evt.BudgetId.ShouldBe(budget.Id);
        evt.CategoryId.ShouldBe(categoryId);
    }
}
