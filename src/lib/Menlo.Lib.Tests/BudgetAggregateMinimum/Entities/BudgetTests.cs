using CSharpFunctionalExtensions;
using Menlo.Lib.Budget.Enums;
using Menlo.Lib.Budget.Errors;
using Menlo.Lib.Budget.Events;
using Menlo.Lib.Budget.ValueObjects;
using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Common.Enums;
using Menlo.Lib.Common.ValueObjects;
using Shouldly;
using BudgetAggregate = Menlo.Lib.Budget.Entities.Budget;
using BudgetCategory = Menlo.Lib.Budget.Entities.BudgetCategory;

namespace Menlo.Lib.Tests.BudgetAggregateMinimum.Entities;

/// <summary>
/// Tests for Budget aggregate root.
/// </summary>
public sealed class BudgetTests
{
    [Fact]
    public void GivenValidBudgetData_WhenCreatingBudget()
    {
        // Arrange
        UserId ownerId = UserId.NewId();
        string name = "January Budget";
        Result<BudgetPeriod, BudgetError> periodResult = BudgetPeriod.Create(2024, 1);
        string currency = "USD";

        // Act
        Result<BudgetAggregate, BudgetError> result = BudgetAggregate.Create(ownerId, name, periodResult.Value, currency);

        // Assert
        ItShouldSucceed(result);
        ItShouldHaveOwnerId(result, ownerId);
        ItShouldHaveName(result, name);
        ItShouldHavePeriod(result, periodResult.Value);
        ItShouldHaveCurrency(result, "USD");
        ItShouldHaveStatus(result, BudgetStatus.Draft);
        ItShouldHaveValidId(result);
        ItShouldHaveBudgetCreatedEvent(result);
    }

    [Fact]
    public void GivenNameWithWhitespace_WhenCreatingBudget()
    {
        // Arrange
        UserId ownerId = UserId.NewId();
        string name = "  January Budget  ";
        Result<BudgetPeriod, BudgetError> periodResult = BudgetPeriod.Create(2024, 1);
        string currency = "USD";

        // Act
        Result<BudgetAggregate, BudgetError> result = BudgetAggregate.Create(ownerId, name, periodResult.Value, currency);

        // Assert
        ItShouldSucceed(result);
        ItShouldHaveName(result, "January Budget");
    }

    [Fact]
    public void GivenLowercaseCurrency_WhenCreatingBudget()
    {
        // Arrange
        UserId ownerId = UserId.NewId();
        string name = "January Budget";
        Result<BudgetPeriod, BudgetError> periodResult = BudgetPeriod.Create(2024, 1);
        string currency = "usd";

        // Act
        Result<BudgetAggregate, BudgetError> result = BudgetAggregate.Create(ownerId, name, periodResult.Value, currency);

        // Assert
        ItShouldSucceed(result);
        ItShouldHaveCurrency(result, "USD");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void GivenEmptyOrWhitespaceName_WhenCreatingBudget(string name)
    {
        // Arrange
        UserId ownerId = UserId.NewId();
        Result<BudgetPeriod, BudgetError> periodResult = BudgetPeriod.Create(2024, 1);
        string currency = "USD";

        // Act
        Result<BudgetAggregate, BudgetError> result = BudgetAggregate.Create(ownerId, name, periodResult.Value, currency);

        // Assert
        ItShouldFail(result);
        ItShouldBeInvalidBudgetDataError(result);
        ItShouldHaveReasonContaining(result, "name");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void GivenEmptyOrWhitespaceCurrency_WhenCreatingBudget(string currency)
    {
        // Arrange
        UserId ownerId = UserId.NewId();
        string name = "January Budget";
        Result<BudgetPeriod, BudgetError> periodResult = BudgetPeriod.Create(2024, 1);

        // Act
        Result<BudgetAggregate, BudgetError> result = BudgetAggregate.Create(ownerId, name, periodResult.Value, currency);

        // Assert
        ItShouldFail(result);
        ItShouldBeInvalidBudgetDataError(result);
        ItShouldHaveReasonContaining(result, "Currency");
    }

    [Theory]
    [InlineData("US")]
    [InlineData("USDD")]
    public void GivenInvalidCurrencyLength_WhenCreatingBudget(string currency)
    {
        // Arrange
        UserId ownerId = UserId.NewId();
        string name = "January Budget";
        Result<BudgetPeriod, BudgetError> periodResult = BudgetPeriod.Create(2024, 1);

        // Act
        Result<BudgetAggregate, BudgetError> result = BudgetAggregate.Create(ownerId, name, periodResult.Value, currency);

        // Assert
        ItShouldFail(result);
        ItShouldBeInvalidBudgetDataError(result);
        ItShouldHaveReasonContaining(result, "3-letter");
    }

    [Fact]
    public void GivenValidBudget_WhenAddingRootCategory()
    {
        // Arrange
        BudgetAggregate budget = CreateTestBudget();
        string categoryName = "Groceries";

        // Act
        Result<BudgetCategory, BudgetError> result = budget.AddCategory(categoryName, "Food and household items");

        // Assert
        ItShouldSucceed(result);
        ItShouldHaveCategoryName(result, categoryName);
        ItShouldBeRootCategory(result);
        ItShouldHaveNullPlannedAmount(result);
        budget.Categories.Count.ShouldBe(1);
        budget.DomainEvents.Count.ShouldBe(2); // BudgetCreated + CategoryAdded
    }

    [Fact]
    public void GivenBudgetWithCategory_WhenAddingDuplicateCategoryName()
    {
        // Arrange
        BudgetAggregate budget = CreateTestBudget();
        budget.AddCategory("Groceries");

        // Act
        Result<BudgetCategory, BudgetError> result = budget.AddCategory("Groceries");

        // Assert
        ItShouldFail(result);
        result.Error.ShouldBeOfType<DuplicateCategoryNameError>();
    }

    [Fact]
    public void GivenBudgetWithCategory_WhenAddingDuplicateCategoryNameDifferentCase()
    {
        // Arrange
        BudgetAggregate budget = CreateTestBudget();
        budget.AddCategory("Groceries");

        // Act
        Result<BudgetCategory, BudgetError> result = budget.AddCategory("GROCERIES");

        // Assert
        ItShouldFail(result);
        result.Error.ShouldBeOfType<DuplicateCategoryNameError>();
    }

    [Fact]
    public void GivenBudgetWithRootCategory_WhenAddingSubcategory()
    {
        // Arrange
        BudgetAggregate budget = CreateTestBudget();
        Result<BudgetCategory, BudgetError> parentResult = budget.AddCategory("Groceries");
        BudgetCategory parent = parentResult.Value;

        // Act
        Result<BudgetCategory, BudgetError> result = budget.AddSubcategory(parent.Id, "Fresh Produce");

        // Assert
        ItShouldSucceed(result);
        ItShouldHaveCategoryName(result, "Fresh Produce");
        result.Value.IsRoot.ShouldBeFalse();
        result.Value.ParentId.ShouldBe(parent.Id);
        parent.Children.Count.ShouldBe(1);
    }

    [Fact]
    public void GivenSubcategory_WhenAddingNestedSubcategory()
    {
        // Arrange
        BudgetAggregate budget = CreateTestBudget();
        Result<BudgetCategory, BudgetError> parentResult = budget.AddCategory("Groceries");
        BudgetCategory parent = parentResult.Value;
        Result<BudgetCategory, BudgetError> childResult = budget.AddSubcategory(parent.Id, "Fresh Produce");
        BudgetCategory child = childResult.Value;

        // Act
        Result<BudgetCategory, BudgetError> result = budget.AddSubcategory(child.Id, "Vegetables");

        // Assert
        ItShouldFail(result);
        result.Error.ShouldBeOfType<MaxDepthExceededError>();
    }

    [Fact]
    public void GivenNonExistentParent_WhenAddingSubcategory()
    {
        // Arrange
        BudgetAggregate budget = CreateTestBudget();
        BudgetCategoryId nonExistentId = BudgetCategoryId.NewId();

        // Act
        Result<BudgetCategory, BudgetError> result = budget.AddSubcategory(nonExistentId, "Fresh Produce");

        // Assert
        ItShouldFail(result);
        result.Error.ShouldBeOfType<CategoryNotFoundError>();
    }

    [Fact]
    public void GivenBudgetWithCategory_WhenSettingPlannedAmount()
    {
        // Arrange
        BudgetAggregate budget = CreateTestBudget();
        Result<BudgetCategory, BudgetError> categoryResult = budget.AddCategory("Groceries");
        BudgetCategory category = categoryResult.Value;
        Money amount = Money.Create(500.00m, "USD").Value;

        // Act
        Result<bool, BudgetError> result = budget.SetPlannedAmount(category.Id, amount);

        // Assert
        ItShouldSucceed(result);
        category.PlannedAmount.ShouldNotBeNull();
        category.PlannedAmount.Value.Amount.ShouldBe(500.00m);
        budget.DomainEvents.OfType<PlannedAmountSetEvent>().Count().ShouldBe(1);
    }

    [Fact]
    public void GivenCategoryWithPlannedAmount_WhenSettingDifferentAmount()
    {
        // Arrange
        BudgetAggregate budget = CreateTestBudget();
        Result<BudgetCategory, BudgetError> categoryResult = budget.AddCategory("Groceries");
        BudgetCategory category = categoryResult.Value;
        Money amount1 = Money.Create(500.00m, "USD").Value;
        Money amount2 = Money.Create(750.00m, "USD").Value;
        budget.SetPlannedAmount(category.Id, amount1);

        // Act
        Result<bool, BudgetError> result = budget.SetPlannedAmount(category.Id, amount2);

        // Assert
        ItShouldSucceed(result);
        category.PlannedAmount.Value.Amount.ShouldBe(750.00m);
    }

    [Fact]
    public void GivenCategory_WhenSettingNegativePlannedAmount()
    {
        // Arrange
        BudgetAggregate budget = CreateTestBudget();
        Result<BudgetCategory, BudgetError> categoryResult = budget.AddCategory("Groceries");
        BudgetCategory category = categoryResult.Value;
        Money amount = Money.Create(-100.00m, "USD").Value;

        // Act
        Result<bool, BudgetError> result = budget.SetPlannedAmount(category.Id, amount);

        // Assert
        ItShouldFail(result);
        result.Error.ShouldBeOfType<InvalidAmountError>();
    }

    [Fact]
    public void GivenCategory_WhenSettingPlannedAmountWithWrongCurrency()
    {
        // Arrange
        BudgetAggregate budget = CreateTestBudget();
        Result<BudgetCategory, BudgetError> categoryResult = budget.AddCategory("Groceries");
        BudgetCategory category = categoryResult.Value;
        Money amount = Money.Create(500.00m, "EUR").Value;

        // Act
        Result<bool, BudgetError> result = budget.SetPlannedAmount(category.Id, amount);

        // Assert
        ItShouldFail(result);
        result.Error.ShouldBeOfType<InvalidAmountError>();
        ((InvalidAmountError)result.Error).Reason.ShouldContain("currency");
    }

    [Fact]
    public void GivenCategoryWithPlannedAmount_WhenClearingPlannedAmount()
    {
        // Arrange
        BudgetAggregate budget = CreateTestBudget();
        Result<BudgetCategory, BudgetError> categoryResult = budget.AddCategory("Groceries");
        BudgetCategory category = categoryResult.Value;
        Money amount = Money.Create(500.00m, "USD").Value;
        budget.SetPlannedAmount(category.Id, amount);

        // Act
        Result<bool, BudgetError> result = budget.ClearPlannedAmount(category.Id);

        // Assert
        ItShouldSucceed(result);
        category.PlannedAmount.ShouldBeNull();
        budget.DomainEvents.OfType<PlannedAmountClearedEvent>().Count().ShouldBe(1);
    }

    [Fact]
    public void GivenBudgetWithCategory_WhenRenamingCategory()
    {
        // Arrange
        BudgetAggregate budget = CreateTestBudget();
        Result<BudgetCategory, BudgetError> categoryResult = budget.AddCategory("Groceries");
        BudgetCategory category = categoryResult.Value;
        string newName = "Food & Groceries";

        // Act
        Result<bool, BudgetError> result = budget.RenameCategory(category.Id, newName);

        // Assert
        ItShouldSucceed(result);
        category.Name.ShouldBe(newName);
        budget.DomainEvents.OfType<CategoryRenamedEvent>().Count().ShouldBe(1);
    }

    [Fact]
    public void GivenTwoCategories_WhenRenamingToDuplicateName()
    {
        // Arrange
        BudgetAggregate budget = CreateTestBudget();
        budget.AddCategory("Groceries");
        Result<BudgetCategory, BudgetError> category2Result = budget.AddCategory("Entertainment");
        BudgetCategory category2 = category2Result.Value;

        // Act
        Result<bool, BudgetError> result = budget.RenameCategory(category2.Id, "Groceries");

        // Assert
        ItShouldFail(result);
        result.Error.ShouldBeOfType<DuplicateCategoryNameError>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void GivenCategory_WhenRenamingToEmptyName(string newName)
    {
        // Arrange
        BudgetAggregate budget = CreateTestBudget();
        Result<BudgetCategory, BudgetError> categoryResult = budget.AddCategory("Groceries");
        BudgetCategory category = categoryResult.Value;

        // Act
        Result<bool, BudgetError> result = budget.RenameCategory(category.Id, newName);

        // Assert
        ItShouldFail(result);
        result.Error.ShouldBeOfType<InvalidBudgetDataError>();
    }

    [Fact]
    public void GivenLeafCategoryWithoutPlannedAmount_WhenRemovingCategory()
    {
        // Arrange
        BudgetAggregate budget = CreateTestBudget();
        Result<BudgetCategory, BudgetError> categoryResult = budget.AddCategory("Groceries");
        BudgetCategory category = categoryResult.Value;

        // Act
        Result<bool, BudgetError> result = budget.RemoveCategory(category.Id);

        // Assert
        ItShouldSucceed(result);
        budget.Categories.Count.ShouldBe(0);
        budget.DomainEvents.OfType<CategoryRemovedEvent>().Count().ShouldBe(1);
    }

    [Fact]
    public void GivenCategoryWithChildren_WhenRemovingCategory()
    {
        // Arrange
        BudgetAggregate budget = CreateTestBudget();
        Result<BudgetCategory, BudgetError> parentResult = budget.AddCategory("Groceries");
        BudgetCategory parent = parentResult.Value;
        budget.AddSubcategory(parent.Id, "Fresh Produce");

        // Act
        Result<bool, BudgetError> result = budget.RemoveCategory(parent.Id);

        // Assert
        ItShouldFail(result);
        result.Error.ShouldBeOfType<CategoryHasChildrenError>();
    }

    [Fact]
    public void GivenCategoryWithPlannedAmount_WhenRemovingCategory()
    {
        // Arrange
        BudgetAggregate budget = CreateTestBudget();
        Result<BudgetCategory, BudgetError> categoryResult = budget.AddCategory("Groceries");
        BudgetCategory category = categoryResult.Value;
        Money amount = Money.Create(500.00m, "USD").Value;
        budget.SetPlannedAmount(category.Id, amount);

        // Act
        Result<bool, BudgetError> result = budget.RemoveCategory(category.Id);

        // Assert
        ItShouldFail(result);
        result.Error.ShouldBeOfType<CategoryHasPlannedAmountError>();
    }

    [Fact]
    public void GivenBudgetWithCategories_WhenCalculatingTotal()
    {
        // Arrange
        BudgetAggregate budget = CreateTestBudget();
        Result<BudgetCategory, BudgetError> category1Result = budget.AddCategory("Groceries");
        Result<BudgetCategory, BudgetError> category2Result = budget.AddCategory("Entertainment");
        Money amount1 = Money.Create(500.00m, "USD").Value;
        Money amount2 = Money.Create(200.00m, "USD").Value;
        budget.SetPlannedAmount(category1Result.Value.Id, amount1);
        budget.SetPlannedAmount(category2Result.Value.Id, amount2);

        // Act
        Money total = budget.GetTotal();

        // Assert
        total.Amount.ShouldBe(700.00m);
        total.Currency.ShouldBe("USD");
    }

    [Fact]
    public void GivenBudgetWithCategoryHierarchy_WhenCalculatingTotal()
    {
        // Arrange
        BudgetAggregate budget = CreateTestBudget();
        Result<BudgetCategory, BudgetError> parentResult = budget.AddCategory("Groceries");
        BudgetCategory parent = parentResult.Value;
        Result<BudgetCategory, BudgetError> child1Result = budget.AddSubcategory(parent.Id, "Fresh Produce");
        Result<BudgetCategory, BudgetError> child2Result = budget.AddSubcategory(parent.Id, "Dairy");

        Money amount1 = Money.Create(300.00m, "USD").Value;
        Money amount2 = Money.Create(150.00m, "USD").Value;
        budget.SetPlannedAmount(child1Result.Value.Id, amount1);
        budget.SetPlannedAmount(child2Result.Value.Id, amount2);

        // Act
        Money total = budget.GetTotal();

        // Assert
        total.Amount.ShouldBe(450.00m);
    }

    [Fact]
    public void GivenDraftBudgetWithValidData_WhenActivating()
    {
        // Arrange
        BudgetAggregate budget = CreateTestBudget();
        Result<BudgetCategory, BudgetError> categoryResult = budget.AddCategory("Groceries");
        Money amount = Money.Create(500.00m, "USD").Value;
        budget.SetPlannedAmount(categoryResult.Value.Id, amount);

        // Act
        Result<bool, BudgetError> result = budget.Activate();

        // Assert
        ItShouldSucceed(result);
        budget.Status.ShouldBe(BudgetStatus.Active);
        budget.DomainEvents.OfType<BudgetActivatedEvent>().Count().ShouldBe(1);
    }

    [Fact]
    public void GivenDraftBudgetWithoutPlannedAmounts_WhenActivating()
    {
        // Arrange
        BudgetAggregate budget = CreateTestBudget();
        budget.AddCategory("Groceries");

        // Act
        Result<bool, BudgetError> result = budget.Activate();

        // Assert
        ItShouldFail(result);
        result.Error.ShouldBeOfType<ActivationValidationError>();
    }

    [Fact]
    public void GivenDraftBudgetWithOnlyZeroPlannedAmounts_WhenActivating()
    {
        // Arrange
        BudgetAggregate budget = CreateTestBudget();
        Result<BudgetCategory, BudgetError> categoryResult = budget.AddCategory("Groceries");
        Money zeroAmount = Money.Zero("USD");
        budget.SetPlannedAmount(categoryResult.Value.Id, zeroAmount);

        // Act
        Result<bool, BudgetError> result = budget.Activate();

        // Assert
        ItShouldFail(result);
        result.Error.ShouldBeOfType<ActivationValidationError>();
    }

    [Fact]
    public void GivenActiveBudget_WhenActivating()
    {
        // Arrange
        BudgetAggregate budget = CreateTestBudget();
        Result<BudgetCategory, BudgetError> categoryResult = budget.AddCategory("Groceries");
        Money amount = Money.Create(500.00m, "USD").Value;
        budget.SetPlannedAmount(categoryResult.Value.Id, amount);
        budget.Activate();

        // Act
        Result<bool, BudgetError> result = budget.Activate();

        // Assert
        ItShouldFail(result);
        result.Error.ShouldBeOfType<InvalidStatusTransitionError>();
    }

    [Fact]
    public void GivenBudget_WhenUpdatingName()
    {
        // Arrange
        BudgetAggregate budget = CreateTestBudget();
        string newName = "February Budget";

        // Act
        Result<bool, BudgetError> result = budget.UpdateName(newName);

        // Assert
        ItShouldSucceed(result);
        budget.Name.ShouldBe(newName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void GivenBudget_WhenUpdatingToEmptyName(string newName)
    {
        // Arrange
        BudgetAggregate budget = CreateTestBudget();

        // Act
        Result<bool, BudgetError> result = budget.UpdateName(newName);

        // Assert
        ItShouldFail(result);
        result.Error.ShouldBeOfType<InvalidBudgetDataError>();
    }

    [Fact]
    public void GivenBudget_WhenAuditingForCreate()
    {
        // Arrange
        BudgetAggregate budget = CreateTestBudget();
        IAuditStampFactory factory = CreateAuditStampFactory();

        // Act
        budget.Audit(factory, AuditOperation.Create);

        // Assert
        ItShouldHaveCreatedBy(budget);
        ItShouldHaveCreatedAt(budget);
        ItShouldHaveModifiedBy(budget);
        ItShouldHaveModifiedAt(budget);
    }

    [Fact]
    public void GivenBudget_WhenAuditingForUpdate()
    {
        // Arrange
        BudgetAggregate budget = CreateTestBudget();
        IAuditStampFactory factory = CreateAuditStampFactory();

        // Act
        budget.Audit(factory, AuditOperation.Update);

        // Assert
        ItShouldNotHaveCreatedBy(budget);
        ItShouldNotHaveCreatedAt(budget);
        ItShouldHaveModifiedBy(budget);
        ItShouldHaveModifiedAt(budget);
    }

    [Fact]
    public void GivenBudget_WhenClearingDomainEvents()
    {
        // Arrange
        BudgetAggregate budget = CreateTestBudget();

        // Act
        budget.ClearDomainEvents();

        // Assert
        budget.DomainEvents.ShouldBeEmpty();
    }

    // Assertion Helpers

    private static void ItShouldSucceed(Result<BudgetAggregate, BudgetError> result)
    {
        result.IsSuccess.ShouldBeTrue();
    }

    private static void ItShouldSucceed(Result<BudgetCategory, BudgetError> result)
    {
        result.IsSuccess.ShouldBeTrue();
    }

    private static void ItShouldSucceed(Result<bool, BudgetError> result)
    {
        result.IsSuccess.ShouldBeTrue();
    }

    private static void ItShouldFail(Result<BudgetAggregate, BudgetError> result)
    {
        result.IsFailure.ShouldBeTrue();
    }

    private static void ItShouldFail(Result<BudgetCategory, BudgetError> result)
    {
        result.IsFailure.ShouldBeTrue();
    }

    private static void ItShouldFail(Result<bool, BudgetError> result)
    {
        result.IsFailure.ShouldBeTrue();
    }

    private static void ItShouldHaveOwnerId(Result<BudgetAggregate, BudgetError> result, UserId expectedOwnerId)
    {
        result.Value.OwnerId.ShouldBe(expectedOwnerId);
    }

    private static void ItShouldHaveName(Result<BudgetAggregate, BudgetError> result, string expectedName)
    {
        result.Value.Name.ShouldBe(expectedName);
    }

    private static void ItShouldHavePeriod(Result<BudgetAggregate, BudgetError> result, BudgetPeriod expectedPeriod)
    {
        result.Value.Period.ShouldBe(expectedPeriod);
    }

    private static void ItShouldHaveCurrency(Result<BudgetAggregate, BudgetError> result, string expectedCurrency)
    {
        result.Value.Currency.ShouldBe(expectedCurrency);
    }

    private static void ItShouldHaveStatus(Result<BudgetAggregate, BudgetError> result, BudgetStatus expectedStatus)
    {
        result.Value.Status.ShouldBe(expectedStatus);
    }

    private static void ItShouldHaveValidId(Result<BudgetAggregate, BudgetError> result)
    {
        result.Value.Id.Value.ShouldNotBe(Guid.Empty);
    }

    private static void ItShouldHaveBudgetCreatedEvent(Result<BudgetAggregate, BudgetError> result)
    {
        result.Value.DomainEvents.Count.ShouldBeGreaterThan(0);
        result.Value.DomainEvents.First().ShouldBeOfType<BudgetCreatedEvent>();
    }

    private static void ItShouldBeInvalidBudgetDataError(Result<BudgetAggregate, BudgetError> result)
    {
        result.Error.ShouldBeOfType<InvalidBudgetDataError>();
    }

    private static void ItShouldHaveReasonContaining(Result<BudgetAggregate, BudgetError> result, string expectedSubstring)
    {
        InvalidBudgetDataError error = (InvalidBudgetDataError)result.Error;
        error.Reason.ShouldContain(expectedSubstring, Case.Insensitive);
    }

    private static void ItShouldHaveCategoryName(Result<BudgetCategory, BudgetError> result, string expectedName)
    {
        result.Value.Name.ShouldBe(expectedName);
    }

    private static void ItShouldBeRootCategory(Result<BudgetCategory, BudgetError> result)
    {
        result.Value.IsRoot.ShouldBeTrue();
        result.Value.ParentId.ShouldBeNull();
    }

    private static void ItShouldHaveNullPlannedAmount(Result<BudgetCategory, BudgetError> result)
    {
        result.Value.PlannedAmount.ShouldBeNull();
    }

    private static void ItShouldHaveCreatedBy(BudgetAggregate budget)
    {
        budget.CreatedBy.ShouldNotBeNull();
    }

    private static void ItShouldHaveCreatedAt(BudgetAggregate budget)
    {
        budget.CreatedAt.ShouldNotBeNull();
    }

    private static void ItShouldHaveModifiedBy(BudgetAggregate budget)
    {
        budget.ModifiedBy.ShouldNotBeNull();
    }

    private static void ItShouldHaveModifiedAt(BudgetAggregate budget)
    {
        budget.ModifiedAt.ShouldNotBeNull();
    }

    private static void ItShouldNotHaveCreatedBy(BudgetAggregate budget)
    {
        budget.CreatedBy.ShouldBeNull();
    }

    private static void ItShouldNotHaveCreatedAt(BudgetAggregate budget)
    {
        budget.CreatedAt.ShouldBeNull();
    }

    // Test Setup Helpers

    private static BudgetAggregate CreateTestBudget()
    {
        UserId ownerId = UserId.NewId();
        Result<BudgetPeriod, BudgetError> periodResult = BudgetPeriod.Create(2024, 1);
        Result<BudgetAggregate, BudgetError> budgetResult = BudgetAggregate.Create(
            ownerId,
            "Test Budget",
            periodResult.Value,
            "USD");
        return budgetResult.Value;
    }

    private sealed class FakeAuditStampFactory : IAuditStampFactory
    {
        private readonly AuditStamp _stamp;

        public FakeAuditStampFactory(AuditStamp stamp)
        {
            _stamp = stamp;
        }

        public AuditStamp CreateStamp() => _stamp;
    }

    private static IAuditStampFactory CreateAuditStampFactory()
    {
        UserId actorId = UserId.NewId();
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;
        return new FakeAuditStampFactory(new AuditStamp(actorId, timestamp));
    }
}
