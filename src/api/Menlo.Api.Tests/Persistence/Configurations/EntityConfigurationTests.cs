using Menlo.Api.Persistence.Data;
using Menlo.Lib.Auth.Entities;
using Menlo.Lib.Auth.ValueObjects;
using Menlo.Lib.Budget.Entities;
using Menlo.Lib.Budget.Enums;
using Menlo.Lib.Budget.ValueObjects;
using Menlo.Lib.Common.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Menlo.Api.Tests.Persistence.Configurations;

/// <summary>
/// Tests for EF Core entity configurations to verify proper mapping.
/// </summary>
public sealed class EntityConfigurationTests : IDisposable
{
    private readonly MenloDbContext _dbContext;

    public EntityConfigurationTests()
    {
        DbContextOptionsBuilder<MenloDbContext> optionsBuilder = new DbContextOptionsBuilder<MenloDbContext>()
            .UseInMemoryDatabase($"EntityConfigurationTests_{Guid.NewGuid()}");

        _dbContext = new MenloDbContext(optionsBuilder.Options);
        _dbContext.Database.EnsureCreated();
    }

    #region User Configuration Tests

    [Fact]
    public void GivenUser_WhenSavingToDatabase()
    {
        // Arrange
        ExternalUserId externalId = new("test-external-id");
        var userResult = User.Create(externalId, "test@example.com", "Test User");
        User user = userResult.Value;

        // Act
        _dbContext.Users.Add(user);
        _dbContext.SaveChanges();

        // Assert
        User? savedUser = _dbContext.Users.FirstOrDefault(u => u.Id == user.Id);

        ItShouldPersistUser(savedUser, user);
        ItShouldPersistUserProperties(savedUser, externalId, "test@example.com", "Test User");
    }

    [Fact]
    public void GivenUser_WhenRoundTripping()
    {
        // Arrange
        ExternalUserId externalId = new("round-trip-test");
        var userResult = User.Create(externalId, "roundtrip@example.com", "Round Trip User");
        User originalUser = userResult.Value;

        _dbContext.Users.Add(originalUser);
        _dbContext.SaveChanges();
        _dbContext.ChangeTracker.Clear(); // Clear tracking to simulate fresh load

        // Act
        User? retrievedUser = _dbContext.Users.FirstOrDefault(u => u.Id == originalUser.Id);

        // Assert
        ItShouldMatchOriginalUser(retrievedUser, originalUser);
    }

    #endregion

    #region Budget Configuration Tests

    [Fact]
    public void GivenBudget_WhenSavingToDatabase()
    {
        // Arrange
        UserId ownerId = UserId.NewId();
        BudgetPeriod period = BudgetPeriod.Create(2024, 12).Value;
        var budgetResult = Budget.Create(ownerId, "Test Budget", period, "ZAR");
        Budget budget = budgetResult.Value;

        // Act
        _dbContext.Budgets.Add(budget);
        _dbContext.SaveChanges();

        // Assert
        Budget? savedBudget = _dbContext.Budgets.FirstOrDefault(b => b.Id == budget.Id);

        ItShouldPersistBudget(savedBudget, budget);
        ItShouldPersistBudgetProperties(savedBudget, ownerId, "Test Budget", period, "ZAR");
    }

    [Fact]
    public void GivenBudgetWithCategories_WhenSavingToDatabase()
    {
        // Arrange
        UserId ownerId = UserId.NewId();
        BudgetPeriod period = BudgetPeriod.Create(2024, 12).Value;
        var budgetResult = Budget.Create(ownerId, "Budget with Categories", period, "USD");
        Budget budget = budgetResult.Value;

        var categoryResult = budget.AddCategory("Groceries", "Food and household items");
        BudgetCategory category = categoryResult.Value;
        Money plannedAmount = Money.Create(500.00m, "USD").Value;
        budget.SetPlannedAmount(category.Id, plannedAmount);

        // Act
        _dbContext.Budgets.Add(budget);
        _dbContext.SaveChanges();

        // Assert
        Budget? savedBudget = _dbContext.Budgets
            .Include(b => b.Categories)
            .FirstOrDefault(b => b.Id == budget.Id);

        ItShouldPersistBudgetWithCategories(savedBudget, budget);
        ItShouldPersistCategoryProperties(savedBudget?.Categories.First(), category, plannedAmount);
    }

    [Fact]
    public void GivenBudgetWithNestedCategories_WhenSavingToDatabase()
    {
        // Arrange
        UserId ownerId = UserId.NewId();
        BudgetPeriod period = BudgetPeriod.Create(2024, 11).Value;
        var budgetResult = Budget.Create(ownerId, "Nested Categories Budget", period, "EUR");
        Budget budget = budgetResult.Value;

        var parentCategoryResult = budget.AddCategory("Food", "Food expenses");
        BudgetCategory parentCategory = parentCategoryResult.Value;
        var childCategoryResult = budget.AddSubcategory(parentCategory.Id, "Groceries", "Grocery shopping");
        BudgetCategory childCategory = childCategoryResult.Value;

        Money plannedAmount = Money.Create(300.00m, "EUR").Value;
        budget.SetPlannedAmount(childCategory.Id, plannedAmount);

        // Act
        _dbContext.Budgets.Add(budget);
        _dbContext.SaveChanges();

        // Assert
        Budget? savedBudget = _dbContext.Budgets
            .Include(b => b.Categories)
            .ThenInclude(c => c.Children)
            .FirstOrDefault(b => b.Id == budget.Id);

        ItShouldPersistNestedCategories(savedBudget, parentCategory, childCategory);
        ItShouldPreserveCategoryHierarchy(savedBudget);
    }

    [Fact]
    public void GivenBudget_WhenRoundTripping()
    {
        // Arrange
        UserId ownerId = UserId.NewId();
        BudgetPeriod period = BudgetPeriod.Create(2024, 10).Value;
        var budgetResult = Budget.Create(ownerId, "Round Trip Budget", period, "GBP");
        Budget originalBudget = budgetResult.Value;

        _dbContext.Budgets.Add(originalBudget);
        _dbContext.SaveChanges();
        _dbContext.ChangeTracker.Clear();

        // Act
        Budget? retrievedBudget = _dbContext.Budgets.FirstOrDefault(b => b.Id == originalBudget.Id);

        // Assert
        ItShouldMatchOriginalBudget(retrievedBudget, originalBudget);
    }

    #endregion

    #region Budget Category Configuration Tests

    [Fact]
    public void GivenComplexBudgetCategory_WhenTestingValueConversion()
    {
        // Arrange
        UserId ownerId = UserId.NewId();
        BudgetPeriod period = BudgetPeriod.Create(2024, 9).Value;
        var budgetResult = Budget.Create(ownerId, "Value Conversion Test", period, "ZAR");
        Budget budget = budgetResult.Value;

        var categoryResult = budget.AddCategory("Complex Category", "Testing complex properties");
        BudgetCategory category = categoryResult.Value;
        Money preciseMoney = Money.Create(123.456789m, "ZAR").Value;
        budget.SetPlannedAmount(category.Id, preciseMoney);

        // Act
        _dbContext.Budgets.Add(budget);
        _dbContext.SaveChanges();
        _dbContext.ChangeTracker.Clear();

        // Assert
        Budget? savedBudget = _dbContext.Budgets
            .Include(b => b.Categories)
            .FirstOrDefault(b => b.Id == budget.Id);

        BudgetCategory? savedCategory = savedBudget?.Categories.FirstOrDefault();
        ItShouldPreservePreciseMoneyValues(savedCategory, preciseMoney);
    }

    #endregion

    #region Assertion Helpers - User

    private static void ItShouldPersistUser(User? savedUser, User originalUser)
    {
        savedUser.ShouldNotBeNull();
        savedUser.Id.ShouldBe(originalUser.Id);
    }

    private static void ItShouldPersistUserProperties(User? savedUser, ExternalUserId expectedExternalId, 
        string expectedEmail, string expectedDisplayName)
    {
        savedUser.ShouldNotBeNull();
        savedUser.ExternalId.ShouldBe(expectedExternalId);
        savedUser.Email.ShouldBe(expectedEmail);
        savedUser.DisplayName.ShouldBe(expectedDisplayName);
    }

    private static void ItShouldMatchOriginalUser(User? retrievedUser, User originalUser)
    {
        retrievedUser.ShouldNotBeNull();
        retrievedUser.Id.ShouldBe(originalUser.Id);
        retrievedUser.ExternalId.ShouldBe(originalUser.ExternalId);
        retrievedUser.Email.ShouldBe(originalUser.Email);
        retrievedUser.DisplayName.ShouldBe(originalUser.DisplayName);
    }

    #endregion

    #region Assertion Helpers - Budget

    private static void ItShouldPersistBudget(Budget? savedBudget, Budget originalBudget)
    {
        savedBudget.ShouldNotBeNull();
        savedBudget.Id.ShouldBe(originalBudget.Id);
    }

    private static void ItShouldPersistBudgetProperties(Budget? savedBudget, UserId expectedOwnerId,
        string expectedName, BudgetPeriod expectedPeriod, string expectedCurrency)
    {
        savedBudget.ShouldNotBeNull();
        savedBudget.OwnerId.ShouldBe(expectedOwnerId);
        savedBudget.Name.ShouldBe(expectedName);
        savedBudget.Period.ShouldBe(expectedPeriod);
        savedBudget.Currency.ShouldBe(expectedCurrency);
        savedBudget.Status.ShouldBe(BudgetStatus.Draft);
    }

    private static void ItShouldPersistBudgetWithCategories(Budget? savedBudget, Budget originalBudget)
    {
        savedBudget.ShouldNotBeNull();
        savedBudget.Id.ShouldBe(originalBudget.Id);
        savedBudget.Categories.ShouldNotBeEmpty();
        savedBudget.Categories.Count.ShouldBe(1);
    }

    private static void ItShouldPersistCategoryProperties(BudgetCategory? savedCategory, BudgetCategory originalCategory, 
        Money expectedAmount)
    {
        savedCategory.ShouldNotBeNull();
        savedCategory.Id.ShouldBe(originalCategory.Id);
        savedCategory.Name.ShouldBe(originalCategory.Name);
        savedCategory.Description.ShouldBe(originalCategory.Description);
        savedCategory.PlannedAmount.ShouldNotBeNull();
        savedCategory.PlannedAmount.ShouldBe(expectedAmount);
    }

    private static void ItShouldPersistNestedCategories(Budget? savedBudget, BudgetCategory parentCategory, 
        BudgetCategory childCategory)
    {
        savedBudget.ShouldNotBeNull();
        savedBudget.Categories.Count.ShouldBe(2);

        BudgetCategory? savedParent = savedBudget.Categories.FirstOrDefault(c => c.Id == parentCategory.Id);
        BudgetCategory? savedChild = savedBudget.Categories.FirstOrDefault(c => c.Id == childCategory.Id);

        savedParent.ShouldNotBeNull();
        savedChild.ShouldNotBeNull();
    }

    private static void ItShouldPreserveCategoryHierarchy(Budget? savedBudget)
    {
        savedBudget.ShouldNotBeNull();
        
        BudgetCategory? parentCategory = savedBudget.Categories.FirstOrDefault(c => c.ParentId == null);
        BudgetCategory? childCategory = savedBudget.Categories.FirstOrDefault(c => c.ParentId != null);

        parentCategory.ShouldNotBeNull();
        childCategory.ShouldNotBeNull();
        childCategory.ParentId.ShouldBe(parentCategory.Id);
        parentCategory.IsRoot.ShouldBeTrue();
        childCategory.IsLeaf.ShouldBeTrue();
    }

    private static void ItShouldMatchOriginalBudget(Budget? retrievedBudget, Budget originalBudget)
    {
        retrievedBudget.ShouldNotBeNull();
        retrievedBudget.Id.ShouldBe(originalBudget.Id);
        retrievedBudget.OwnerId.ShouldBe(originalBudget.OwnerId);
        retrievedBudget.Name.ShouldBe(originalBudget.Name);
        retrievedBudget.Period.ShouldBe(originalBudget.Period);
        retrievedBudget.Currency.ShouldBe(originalBudget.Currency);
        retrievedBudget.Status.ShouldBe(originalBudget.Status);
    }

    private static void ItShouldPreservePreciseMoneyValues(BudgetCategory? savedCategory, Money expectedMoney)
    {
        savedCategory.ShouldNotBeNull();
        savedCategory.PlannedAmount.ShouldNotBeNull();
        savedCategory.PlannedAmount.Value.Amount.ShouldBe(expectedMoney.Amount);
        savedCategory.PlannedAmount.Value.Currency.ShouldBe(expectedMoney.Currency);
    }

    #endregion

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}