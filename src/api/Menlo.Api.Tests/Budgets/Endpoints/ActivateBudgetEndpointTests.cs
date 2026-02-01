using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Menlo.Api.Persistence.Data;
using Menlo.Api.Tests.Fixtures;
using Menlo.Lib.Budget.Models;
using Menlo.Lib.Common.ValueObjects;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using BudgetAggregate = Menlo.Lib.Budget.Entities.Budget;
using BudgetPeriod = Menlo.Lib.Budget.ValueObjects.BudgetPeriod;
using Money = Menlo.Lib.Common.ValueObjects.Money;

namespace Menlo.Api.Tests.Budgets.Endpoints;

/// <summary>
/// Tests for ActivateBudgetEndpoint.
/// </summary>
public sealed class ActivateBudgetEndpointTests(TestWebApplicationFactory factory)
    : TestFixture, IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory = factory;

    private static JsonSerializerOptions JsonOptions { get; } = new()
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public async Task GivenDraftBudgetWithCategories_WhenActivating()
    {
        // Arrange
        HttpClient client = _factory.CreateClient();
        Guid budgetId = await CreateBudgetWithCategory();

        // Act
        HttpResponseMessage response = await client.PostAsync(
            $"/api/budgets/{budgetId}/activate",
            null,
            TestContext.Current.CancellationToken);

        // Assert status first to get useful error messages
        string rawContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        response.IsSuccessStatusCode.ShouldBeTrue($"Expected success but got {response.StatusCode}. Response: {rawContent}");

        BudgetResponse? budgetResponse = JsonSerializer.Deserialize<BudgetResponse>(rawContent, JsonOptions);

        ItShouldReturnBudgetResponse(budgetResponse);
        ItShouldHaveActiveStatus(budgetResponse);
        ItShouldHaveCorrectId(budgetResponse, budgetId);
    }

    [Fact]
    public async Task GivenNonExistentBudget_WhenActivating()
    {
        // Arrange
        HttpClient client = _factory.CreateClient();
        Guid nonExistentId = Guid.NewGuid();

        // Act
        HttpResponseMessage response = await client.PostAsync(
            $"/api/budgets/{nonExistentId}/activate",
            null,
            TestContext.Current.CancellationToken);

        ProblemDetails? problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(
            TestContext.Current.CancellationToken);

        // Assert
        ItShouldHaveNotFoundStatus(response);
        ItShouldReturnProblemDetails(problemDetails);
        ItShouldHaveBudgetNotFoundError(problemDetails);
    }

    [Fact]
    public async Task GivenBudgetOwnedByDifferentUser_WhenActivating()
    {
        // Arrange
        HttpClient client = _factory.CreateClient();
        Guid budgetId = await CreateBudgetForDifferentUser();

        // Act
        HttpResponseMessage response = await client.PostAsync(
            $"/api/budgets/{budgetId}/activate",
            null,
            TestContext.Current.CancellationToken);

        ProblemDetails? problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(
            TestContext.Current.CancellationToken);

        // Assert - Should return 404 to prevent information disclosure
        ItShouldHaveNotFoundStatus(response);
        ItShouldReturnProblemDetails(problemDetails);
        ItShouldHaveBudgetNotFoundError(problemDetails);
    }

    [Fact]
    public async Task GivenActiveBudget_WhenActivating()
    {
        // Arrange
        HttpClient client = _factory.CreateClient();
        Guid budgetId = await CreateActiveBudget();

        // Act
        HttpResponseMessage response = await client.PostAsync(
            $"/api/budgets/{budgetId}/activate",
            null,
            TestContext.Current.CancellationToken);

        ProblemDetails? problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(
            TestContext.Current.CancellationToken);

        // Assert
        ItShouldHaveBadRequestStatus(response);
        ItShouldReturnProblemDetails(problemDetails);
        ItShouldHaveInvalidStatusTransitionError(problemDetails);
    }

    [Fact]
    public async Task GivenBudgetWithNoCategories_WhenActivating()
    {
        // Arrange
        HttpClient client = _factory.CreateClient();
        Guid budgetId = await CreateBudgetWithoutCategories();

        // Act
        HttpResponseMessage response = await client.PostAsync(
            $"/api/budgets/{budgetId}/activate",
            null,
            TestContext.Current.CancellationToken);

        ProblemDetails? problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(
            TestContext.Current.CancellationToken);

        // Assert
        ItShouldHaveBadRequestStatus(response);
        ItShouldReturnProblemDetails(problemDetails);
        ItShouldHaveActivationValidationError(problemDetails);
    }

    [Fact]
    public async Task GivenBudgetWithCategoriesButNoPlannedAmounts_WhenActivating()
    {
        // Arrange
        HttpClient client = _factory.CreateClient();
        Guid budgetId = await CreateBudgetWithCategoryWithoutPlannedAmount();

        // Act
        HttpResponseMessage response = await client.PostAsync(
            $"/api/budgets/{budgetId}/activate",
            null,
            TestContext.Current.CancellationToken);

        ProblemDetails? problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(
            TestContext.Current.CancellationToken);

        // Assert
        ItShouldHaveBadRequestStatus(response);
        ItShouldReturnProblemDetails(problemDetails);
        ItShouldHaveActivationValidationError(problemDetails);
    }

    [Fact]
    public async Task GivenBudgetWithOnlyZeroPlannedAmounts_WhenActivating()
    {
        // Arrange
        HttpClient client = _factory.CreateClient();
        Guid budgetId = await CreateBudgetWithZeroPlannedAmount();

        // Act
        HttpResponseMessage response = await client.PostAsync(
            $"/api/budgets/{budgetId}/activate",
            null,
            TestContext.Current.CancellationToken);

        ProblemDetails? problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(
            TestContext.Current.CancellationToken);

        // Assert
        ItShouldHaveBadRequestStatus(response);
        ItShouldReturnProblemDetails(problemDetails);
        ItShouldHaveActivationValidationError(problemDetails);
    }

    // Assertion Helpers

    private static void ItShouldReturnBudgetResponse(BudgetResponse? budgetResponse)
    {
        budgetResponse.ShouldNotBeNull();
    }

    private static void ItShouldHaveActiveStatus(BudgetResponse? budgetResponse)
    {
        budgetResponse.ShouldNotBeNull();
        budgetResponse.Status.ShouldBe("Active");
    }

    private static void ItShouldHaveCorrectId(BudgetResponse? budgetResponse, Guid expectedId)
    {
        budgetResponse.ShouldNotBeNull();
        budgetResponse.Id.ShouldBe(expectedId);
    }

    private static void ItShouldHaveNotFoundStatus(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private static void ItShouldHaveBadRequestStatus(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    private static void ItShouldReturnProblemDetails(ProblemDetails? problemDetails)
    {
        problemDetails.ShouldNotBeNull();
    }

    private static void ItShouldHaveBudgetNotFoundError(ProblemDetails? problemDetails)
    {
        problemDetails.ShouldNotBeNull();
        problemDetails.Extensions.ShouldContainKey("errorCode");
        problemDetails.Extensions["errorCode"]?.ToString().ShouldBe("BUDGET_NOT_FOUND");
    }

    private static void ItShouldHaveInvalidStatusTransitionError(ProblemDetails? problemDetails)
    {
        problemDetails.ShouldNotBeNull();
        problemDetails.Extensions.ShouldContainKey("errorCode");
        problemDetails.Extensions["errorCode"]?.ToString().ShouldBe("Budget.InvalidStatusTransition");
    }

    private static void ItShouldHaveActivationValidationError(ProblemDetails? problemDetails)
    {
        problemDetails.ShouldNotBeNull();
        problemDetails.Extensions.ShouldContainKey("errorCode");
        problemDetails.Extensions["errorCode"]?.ToString().ShouldBe("Budget.ActivationFailed");
    }

    // Test Data Setup Helpers

    private async Task<Guid> CreateBudgetWithCategory()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        MenloDbContext dbContext = scope.ServiceProvider.GetRequiredService<MenloDbContext>();

        UserId ownerId = new(Guid.Parse(TestAuthHandler.DefaultUserId));
        BudgetPeriod period = BudgetPeriod.Create(2024, 1).Value;
        BudgetAggregate budget = BudgetAggregate.Create(
            ownerId,
            "Test Budget",
            period,
            "USD").Value;

        var categoryResult = budget.AddCategory("Groceries", "Food and household items");
        Money amount = Money.Create(500.00m, "USD").Value;
        budget.SetPlannedAmount(categoryResult.Value.Id, amount);

        dbContext.Budgets.Add(budget);
        await dbContext.SaveChangesAsync();

        return budget.Id.Value;
    }

    private async Task<Guid> CreateActiveBudget()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        MenloDbContext dbContext = scope.ServiceProvider.GetRequiredService<MenloDbContext>();

        UserId ownerId = new(Guid.Parse(TestAuthHandler.DefaultUserId));
        BudgetPeriod period = BudgetPeriod.Create(2024, 2).Value;
        BudgetAggregate budget = BudgetAggregate.Create(
            ownerId,
            "Active Budget",
            period,
            "USD").Value;

        var categoryResult = budget.AddCategory("Groceries");
        Money amount = Money.Create(500.00m, "USD").Value;
        budget.SetPlannedAmount(categoryResult.Value.Id, amount);
        budget.Activate();

        dbContext.Budgets.Add(budget);
        await dbContext.SaveChangesAsync();

        return budget.Id.Value;
    }

    private async Task<Guid> CreateBudgetWithoutCategories()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        MenloDbContext dbContext = scope.ServiceProvider.GetRequiredService<MenloDbContext>();

        UserId ownerId = new(Guid.Parse(TestAuthHandler.DefaultUserId));
        BudgetPeriod period = BudgetPeriod.Create(2024, 3).Value;
        BudgetAggregate budget = BudgetAggregate.Create(
            ownerId,
            "Empty Budget",
            period,
            "USD").Value;

        dbContext.Budgets.Add(budget);
        await dbContext.SaveChangesAsync();

        return budget.Id.Value;
    }

    private async Task<Guid> CreateBudgetWithCategoryWithoutPlannedAmount()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        MenloDbContext dbContext = scope.ServiceProvider.GetRequiredService<MenloDbContext>();

        UserId ownerId = new(Guid.Parse(TestAuthHandler.DefaultUserId));
        BudgetPeriod period = BudgetPeriod.Create(2024, 4).Value;
        BudgetAggregate budget = BudgetAggregate.Create(
            ownerId,
            "Budget Without Amounts",
            period,
            "USD").Value;

        budget.AddCategory("Groceries");

        dbContext.Budgets.Add(budget);
        await dbContext.SaveChangesAsync();

        return budget.Id.Value;
    }

    private async Task<Guid> CreateBudgetWithZeroPlannedAmount()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        MenloDbContext dbContext = scope.ServiceProvider.GetRequiredService<MenloDbContext>();

        UserId ownerId = new(Guid.Parse(TestAuthHandler.DefaultUserId));
        BudgetPeriod period = BudgetPeriod.Create(2024, 5).Value;
        BudgetAggregate budget = BudgetAggregate.Create(
            ownerId,
            "Budget With Zero Amount",
            period,
            "USD").Value;

        var categoryResult = budget.AddCategory("Groceries");
        Money zeroAmount = Money.Zero("USD");
        budget.SetPlannedAmount(categoryResult.Value.Id, zeroAmount);

        dbContext.Budgets.Add(budget);
        await dbContext.SaveChangesAsync();

        return budget.Id.Value;
    }

    private async Task<Guid> CreateBudgetForDifferentUser()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        MenloDbContext dbContext = scope.ServiceProvider.GetRequiredService<MenloDbContext>();

        // Create budget for a different user
        UserId differentUserId = UserId.NewId();
        BudgetPeriod period = BudgetPeriod.Create(2024, 6).Value;
        BudgetAggregate budget = BudgetAggregate.Create(
            differentUserId,
            "Other User Budget",
            period,
            "USD").Value;

        var categoryResult = budget.AddCategory("Groceries");
        Money amount = Money.Create(500.00m, "USD").Value;
        budget.SetPlannedAmount(categoryResult.Value.Id, amount);

        dbContext.Budgets.Add(budget);
        await dbContext.SaveChangesAsync();

        return budget.Id.Value;
    }
}
