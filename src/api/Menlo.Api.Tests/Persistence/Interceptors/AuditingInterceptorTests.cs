using Menlo.Api.Persistence.Data;
using Menlo.Api.Persistence.Interceptors;
using Menlo.Lib.Auth.Entities;
using Menlo.Lib.Auth.ValueObjects;
using Menlo.Lib.Budget.Entities;
using Menlo.Lib.Budget.Enums;
using Menlo.Lib.Budget.ValueObjects;
using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Common.Enums;
using Menlo.Lib.Common.ValueObjects;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Shouldly;

namespace Menlo.Api.Tests.Persistence.Interceptors;

/// <summary>
/// Tests for AuditingInterceptor.
/// </summary>
public sealed class AuditingInterceptorTests : IDisposable
{
    private readonly MenloDbContext _dbContext;
    private readonly IAuditStampFactory _mockFactory;
    private readonly UserId _testUserId;
    private readonly DateTimeOffset _testTimestamp;

    public AuditingInterceptorTests()
    {
        _testUserId = UserId.NewId();
        _testTimestamp = DateTimeOffset.UtcNow;
        _mockFactory = CreateMockAuditStampFactory(_testUserId, _testTimestamp);

        AuditingInterceptor interceptor = new(_mockFactory);

        DbContextOptionsBuilder<MenloDbContext> optionsBuilder = new DbContextOptionsBuilder<MenloDbContext>()
            .UseInMemoryDatabase($"AuditingInterceptorTests_{Guid.NewGuid()}")
            .AddInterceptors(interceptor);

        _dbContext = new MenloDbContext(optionsBuilder.Options);
    }

    [Fact]
    public void GivenNewUser_WhenSavingChanges()
    {
        ExternalUserId externalId = new("external-123");
        var userResult = User.Create(externalId, "test@example.com", "Test User");
        User user = userResult.Value;

        _dbContext.Users.Add(user);
        _dbContext.SaveChanges();

        ItShouldHaveCreatedBy(user);
        ItShouldHaveCreatedAt(user);
        ItShouldHaveModifiedBy(user);
        ItShouldHaveModifiedAt(user);
        ItShouldHaveCreatedByEqualToTestUser(user);
        ItShouldHaveCreatedAtEqualToTestTimestamp(user);
        ItShouldHaveModifiedByEqualToTestUser(user);
        ItShouldHaveModifiedAtEqualToTestTimestamp(user);
        ItShouldHaveCalledAuditStampFactory();
    }

    [Fact]
    public async Task GivenNewUser_WhenSavingChangesAsync()
    {
        ExternalUserId externalId = new("external-456");
        var userResult = User.Create(externalId, "async@example.com", "Async User");
        User user = userResult.Value;

        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        ItShouldHaveCreatedBy(user);
        ItShouldHaveCreatedAt(user);
        ItShouldHaveModifiedBy(user);
        ItShouldHaveModifiedAt(user);
        ItShouldHaveCreatedByEqualToTestUser(user);
        ItShouldHaveCreatedAtEqualToTestTimestamp(user);
        ItShouldHaveModifiedByEqualToTestUser(user);
        ItShouldHaveModifiedAtEqualToTestTimestamp(user);
        ItShouldHaveCalledAuditStampFactory();
    }

    [Fact]
    public void GivenExistingUser_WhenUpdating()
    {
        ExternalUserId externalId = new("external-789");
        var userResult = User.Create(externalId, "update@example.com", "Update User");
        User user = userResult.Value;

        _dbContext.Users.Add(user);
        _dbContext.SaveChanges();

        // Reset the mock to track only update calls
        _mockFactory.ClearReceivedCalls();

        // Modify the user to trigger an update
        user.RecordLogin(DateTimeOffset.UtcNow);
        _dbContext.SaveChanges();

        ItShouldNotHaveCreatedByFromUpdate(user);
        ItShouldNotHaveCreatedAtFromUpdate(user);
        ItShouldHaveModifiedBy(user);
        ItShouldHaveModifiedAt(user);
        ItShouldHaveModifiedByEqualToTestUser(user);
        ItShouldHaveModifiedAtEqualToTestTimestamp(user);
        ItShouldHaveCalledAuditStampFactory();
    }

    [Fact]
    public async Task GivenExistingUser_WhenUpdatingAsync()
    {
        ExternalUserId externalId = new("external-abc");
        var userResult = User.Create(externalId, "updateasync@example.com", "Update Async User");
        User user = userResult.Value;

        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        // Reset the mock to track only update calls
        _mockFactory.ClearReceivedCalls();

        // Modify the user to trigger an update
        user.RecordLogin(DateTimeOffset.UtcNow);
        await _dbContext.SaveChangesAsync();

        ItShouldNotHaveCreatedByFromUpdate(user);
        ItShouldNotHaveCreatedAtFromUpdate(user);
        ItShouldHaveModifiedBy(user);
        ItShouldHaveModifiedAt(user);
        ItShouldHaveModifiedByEqualToTestUser(user);
        ItShouldHaveModifiedAtEqualToTestTimestamp(user);
        ItShouldHaveCalledAuditStampFactory();
    }

    [Fact]
    public void GivenMultipleNewEntities_WhenSavingChanges()
    {
        var user1Result = User.Create(new ExternalUserId("external-1"), "user1@example.com", "User 1");
        User user1 = user1Result.Value;

        var user2Result = User.Create(new ExternalUserId("external-2"), "user2@example.com", "User 2");
        User user2 = user2Result.Value;

        var budgetResult = Budget.Create(
            _testUserId,
            "Test Budget",
            new BudgetPeriod(2026, 1),
            "USD");
        Budget budget = budgetResult.Value;

        _dbContext.Users.Add(user1);
        _dbContext.Users.Add(user2);
        _dbContext.Budgets.Add(budget);
        _dbContext.SaveChanges();

        ItShouldHaveCreatedBy(user1);
        ItShouldHaveCreatedAt(user1);
        ItShouldHaveModifiedBy(user1);
        ItShouldHaveModifiedAt(user1);

        ItShouldHaveCreatedBy(user2);
        ItShouldHaveCreatedAt(user2);
        ItShouldHaveModifiedBy(user2);
        ItShouldHaveModifiedAt(user2);

        ItShouldHaveCreatedBy(budget);
        ItShouldHaveCreatedAt(budget);
        ItShouldHaveModifiedBy(budget);
        ItShouldHaveModifiedAt(budget);

        ItShouldHaveCalledAuditStampFactoryMultipleTimes(3);
    }

    [Fact]
    public void GivenMixOfNewAndUpdatedEntities_WhenSavingChanges()
    {
        var existingUserResult = User.Create(new ExternalUserId("external-old"), "old@example.com", "Old User");
        User existingUser = existingUserResult.Value;

        _dbContext.Users.Add(existingUser);
        _dbContext.SaveChanges();

        // Reset the mock to track only the next save
        _mockFactory.ClearReceivedCalls();

        var newUserResult = User.Create(new ExternalUserId("external-new"), "new@example.com", "New User");
        User newUser = newUserResult.Value;

        _dbContext.Users.Add(newUser);
        existingUser.RecordLogin(DateTimeOffset.UtcNow);
        _dbContext.SaveChanges();

        // New user should have all audit fields set
        ItShouldHaveCreatedBy(newUser);
        ItShouldHaveCreatedAt(newUser);
        ItShouldHaveModifiedBy(newUser);
        ItShouldHaveModifiedAt(newUser);

        // Existing user should only have Modified fields updated
        ItShouldNotHaveCreatedByFromUpdate(existingUser);
        ItShouldNotHaveCreatedAtFromUpdate(existingUser);
        ItShouldHaveModifiedBy(existingUser);
        ItShouldHaveModifiedAt(existingUser);

        // Should have called factory for both entities
        ItShouldHaveCalledAuditStampFactoryMultipleTimes(2);
    }

    [Fact]
    public void GivenNewBudget_WhenSavingChanges()
    {
        var budgetResult = Budget.Create(
            _testUserId,
            "Monthly Budget",
            new BudgetPeriod(2026, 1),
            "USD");
        Budget budget = budgetResult.Value;

        _dbContext.Budgets.Add(budget);
        _dbContext.SaveChanges();

        ItShouldHaveCreatedBy(budget);
        ItShouldHaveCreatedAt(budget);
        ItShouldHaveModifiedBy(budget);
        ItShouldHaveModifiedAt(budget);
        ItShouldHaveCalledAuditStampFactory();
    }

    [Fact]
    public void GivenExistingBudget_WhenUpdating()
    {
        var budgetResult = Budget.Create(
            _testUserId,
            "Original Budget",
            new BudgetPeriod(2026, 1),
            "USD");
        Budget budget = budgetResult.Value;

        _dbContext.Budgets.Add(budget);
        _dbContext.SaveChanges();

        // Reset the mock to track only update calls
        _mockFactory.ClearReceivedCalls();

        // Modify the budget to trigger an update
        var renameResult = budget.UpdateName("Updated Budget");
        _dbContext.SaveChanges();

        ItShouldNotHaveCreatedByFromUpdate(budget);
        ItShouldNotHaveCreatedAtFromUpdate(budget);
        ItShouldHaveModifiedBy(budget);
        ItShouldHaveModifiedAt(budget);
        ItShouldHaveCalledAuditStampFactory();
    }

    [Fact]
    public void GivenUnchangedEntity_WhenSavingChanges()
    {
        ExternalUserId externalId = new("external-unchanged");
        var userResult = User.Create(externalId, "unchanged@example.com", "Unchanged User");
        User user = userResult.Value;

        _dbContext.Users.Add(user);
        _dbContext.SaveChanges();

        // Reset the mock to track only the next save
        _mockFactory.ClearReceivedCalls();

        // Save changes without modifying the entity
        _dbContext.SaveChanges();

        ItShouldNotHaveCalledAuditStampFactoryForUnchangedEntity();
    }

    [Fact]
    public void GivenDeletedEntity_WhenSavingChanges()
    {
        ExternalUserId externalId = new("external-delete");
        var userResult = User.Create(externalId, "delete@example.com", "Delete User");
        User user = userResult.Value;

        _dbContext.Users.Add(user);
        _dbContext.SaveChanges();

        // Reset the mock to track only the next save
        _mockFactory.ClearReceivedCalls();

        // Delete the entity
        _dbContext.Users.Remove(user);
        _dbContext.SaveChanges();

        ItShouldNotHaveCalledAuditStampFactoryForDeletedEntity();
    }

    [Fact]
    public void GivenCreateOperation_WhenAuditingViaInterceptor()
    {
        ExternalUserId externalId = new("external-create-op");
        var userResult = User.Create(externalId, "createop@example.com", "Create Op User");
        User user = userResult.Value;

        _dbContext.Users.Add(user);
        _dbContext.SaveChanges();

        ItShouldHavePassedCreateOperationToFactory(user);
    }

    [Fact]
    public void GivenUpdateOperation_WhenAuditingViaInterceptor()
    {
        ExternalUserId externalId = new("external-update-op");
        var userResult = User.Create(externalId, "updateop@example.com", "Update Op User");
        User user = userResult.Value;

        _dbContext.Users.Add(user);
        _dbContext.SaveChanges();

        // Verify create was called first
        ItShouldHavePassedCreateOperationToFactory(user);

        // Reset and test update
        _mockFactory.ClearReceivedCalls();
        user.RecordLogin(DateTimeOffset.UtcNow);
        _dbContext.SaveChanges();

        ItShouldHavePassedUpdateOperationToFactory(user);
    }

    // Assertion Helpers
    private static void ItShouldHaveCreatedBy(IAuditable entity)
    {
        entity.CreatedBy.ShouldNotBeNull();
    }

    private static void ItShouldHaveCreatedAt(IAuditable entity)
    {
        entity.CreatedAt.ShouldNotBeNull();
    }

    private static void ItShouldHaveModifiedBy(IAuditable entity)
    {
        entity.ModifiedBy.ShouldNotBeNull();
    }

    private static void ItShouldHaveModifiedAt(IAuditable entity)
    {
        entity.ModifiedAt.ShouldNotBeNull();
    }

    private void ItShouldHaveCreatedByEqualToTestUser(IAuditable entity)
    {
        entity.CreatedBy.ShouldBe(_testUserId);
    }

    private void ItShouldHaveCreatedAtEqualToTestTimestamp(IAuditable entity)
    {
        entity.CreatedAt.ShouldBe(_testTimestamp);
    }

    private void ItShouldHaveModifiedByEqualToTestUser(IAuditable entity)
    {
        entity.ModifiedBy.ShouldBe(_testUserId);
    }

    private void ItShouldHaveModifiedAtEqualToTestTimestamp(IAuditable entity)
    {
        entity.ModifiedAt.ShouldBe(_testTimestamp);
    }

    private static void ItShouldNotHaveCreatedByFromUpdate(IAuditable entity)
    {
        // Created fields should still be set from initial creation, not null
        entity.CreatedBy.ShouldNotBeNull();
    }

    private static void ItShouldNotHaveCreatedAtFromUpdate(IAuditable entity)
    {
        // Created fields should still be set from initial creation, not null
        entity.CreatedAt.ShouldNotBeNull();
    }

    private void ItShouldHaveCalledAuditStampFactory()
    {
        _mockFactory.Received(1).CreateStamp();
    }

    private void ItShouldHaveCalledAuditStampFactoryMultipleTimes(int expectedCount)
    {
        _mockFactory.Received(expectedCount).CreateStamp();
    }

    private void ItShouldNotHaveCalledAuditStampFactoryForUnchangedEntity()
    {
        _mockFactory.DidNotReceive().CreateStamp();
    }

    private void ItShouldNotHaveCalledAuditStampFactoryForDeletedEntity()
    {
        _mockFactory.DidNotReceive().CreateStamp();
    }

    private static void ItShouldHavePassedCreateOperationToFactory(IAuditable entity)
    {
        // When create operation is used, both Created and Modified fields are set
        entity.CreatedBy.ShouldNotBeNull();
        entity.CreatedAt.ShouldNotBeNull();
        entity.ModifiedBy.ShouldNotBeNull();
        entity.ModifiedAt.ShouldNotBeNull();
    }

    private static void ItShouldHavePassedUpdateOperationToFactory(IAuditable entity)
    {
        // When update operation is used, only Modified fields are updated
        // Created fields should still be set from initial creation
        entity.CreatedBy.ShouldNotBeNull();
        entity.CreatedAt.ShouldNotBeNull();
        entity.ModifiedBy.ShouldNotBeNull();
        entity.ModifiedAt.ShouldNotBeNull();
    }

    // Test Setup Helpers
    private static IAuditStampFactory CreateMockAuditStampFactory(UserId userId, DateTimeOffset timestamp)
    {
        IAuditStampFactory factory = Substitute.For<IAuditStampFactory>();
        AuditStamp stamp = new(userId, timestamp);
        factory.CreateStamp().Returns(stamp);
        return factory;
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
    }
}
