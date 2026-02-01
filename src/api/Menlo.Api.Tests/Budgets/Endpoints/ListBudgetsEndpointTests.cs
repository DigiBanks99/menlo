using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Menlo.Api.Persistence.Data;
using Menlo.Api.Tests.Fixtures;
using Menlo.Lib.Budget.Models;
using Menlo.Lib.Common.ValueObjects;
using Microsoft.Extensions.DependencyInjection;
using BudgetAggregate = Menlo.Lib.Budget.Entities.Budget;
using BudgetPeriod = Menlo.Lib.Budget.ValueObjects.BudgetPeriod;

namespace Menlo.Api.Tests.Budgets.Endpoints;

/// <summary>
/// Tests for ListBudgetsEndpoint.
/// </summary>
public sealed class ListBudgetsEndpointTests(TestWebApplicationFactory factory)
    : TestFixture, IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory = factory;

    private static JsonSerializerOptions JsonOptions { get; } = new()
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public async Task GivenNoBudgets_WhenListing()
    {
        // Arrange
        HttpClient client = _factory.CreateClient();

        // Act
        HttpResponseMessage response = await client.GetAsync(
            "/api/budgets",
            TestContext.Current.CancellationToken);

        // Assert
        string rawContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        response.IsSuccessStatusCode.ShouldBeTrue($"Expected success but got {response.StatusCode}. Response: {rawContent}");

        IReadOnlyList<BudgetSummaryResponse>? budgets = JsonSerializer.Deserialize<IReadOnlyList<BudgetSummaryResponse>>(rawContent, JsonOptions);

        ItShouldReturnOkStatus(response);
        ItShouldReturnEmptyList(budgets);
    }

    [Fact]
    public async Task GivenMultipleBudgets_WhenListing()
    {
        // Arrange
        HttpClient client = _factory.CreateClient();
        
        // Create test budgets
        await CreateTestBudget(client, "Budget 2023-01", 2023, 1, "ZAR");
        await CreateTestBudget(client, "Budget 2023-12", 2023, 12, "USD");
        await CreateTestBudget(client, "Budget 2024-03", 2024, 3, "ZAR");

        // Act
        HttpResponseMessage response = await client.GetAsync(
            "/api/budgets",
            TestContext.Current.CancellationToken);

        string rawContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        IReadOnlyList<BudgetSummaryResponse>? budgets = JsonSerializer.Deserialize<IReadOnlyList<BudgetSummaryResponse>>(rawContent, JsonOptions);

        // Assert
        ItShouldReturnOkStatus(response);
        ItShouldReturnExpectedCount(budgets, 3);
        ItShouldBeOrderedByPeriodDescending(budgets);
    }

    [Fact]
    public async Task GivenYearFilter_WhenListing()
    {
        // Arrange
        HttpClient client = _factory.CreateClient();
        
        // Create test budgets for different years
        await CreateTestBudget(client, "Budget 2023-01", 2023, 1, "ZAR");
        await CreateTestBudget(client, "Budget 2024-01", 2024, 1, "ZAR");
        await CreateTestBudget(client, "Budget 2024-06", 2024, 6, "USD");

        // Act - Filter by year 2024
        HttpResponseMessage response = await client.GetAsync(
            "/api/budgets?year=2024",
            TestContext.Current.CancellationToken);

        string rawContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        IReadOnlyList<BudgetSummaryResponse>? budgets = JsonSerializer.Deserialize<IReadOnlyList<BudgetSummaryResponse>>(rawContent, JsonOptions);

        // Assert
        ItShouldReturnOkStatus(response);
        ItShouldReturnExpectedCount(budgets, 2);
        ItShouldOnlyContainYear(budgets, 2024);
    }

    [Fact]
    public async Task GivenStatusFilter_WhenListing()
    {
        // Arrange
        HttpClient client = _factory.CreateClient();
        
        // Create draft budget
        Guid draftBudgetId = await CreateTestBudget(client, "Draft Budget", 2024, 1, "ZAR");
        
        // Create and activate another budget
        Guid activeBudgetId = await CreateTestBudget(client, "Active Budget", 2024, 2, "ZAR");
        await client.PostAsync($"/api/budgets/{activeBudgetId}/activate", null, TestContext.Current.CancellationToken);

        // Act - Filter by Draft status
        HttpResponseMessage response = await client.GetAsync(
            "/api/budgets?status=Draft",
            TestContext.Current.CancellationToken);

        string rawContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        IReadOnlyList<BudgetSummaryResponse>? budgets = JsonSerializer.Deserialize<IReadOnlyList<BudgetSummaryResponse>>(rawContent, JsonOptions);

        // Assert
        ItShouldReturnOkStatus(response);
        ItShouldReturnExpectedCount(budgets, 1);
        ItShouldOnlyContainStatus(budgets, "Draft");
    }

    [Fact]
    public async Task GivenYearAndStatusFilters_WhenListing()
    {
        // Arrange
        HttpClient client = _factory.CreateClient();
        
        // Create budgets with different combinations
        await CreateTestBudget(client, "Budget 2023-Draft", 2023, 1, "ZAR");
        Guid budget2024Draft = await CreateTestBudget(client, "Budget 2024-Draft", 2024, 1, "ZAR");
        Guid budget2024Active = await CreateTestBudget(client, "Budget 2024-Active", 2024, 2, "ZAR");
        await client.PostAsync($"/api/budgets/{budget2024Active}/activate", null, TestContext.Current.CancellationToken);

        // Act - Filter by year 2024 and Draft status
        HttpResponseMessage response = await client.GetAsync(
            "/api/budgets?year=2024&status=Draft",
            TestContext.Current.CancellationToken);

        string rawContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        IReadOnlyList<BudgetSummaryResponse>? budgets = JsonSerializer.Deserialize<IReadOnlyList<BudgetSummaryResponse>>(rawContent, JsonOptions);

        // Assert
        ItShouldReturnOkStatus(response);
        ItShouldReturnExpectedCount(budgets, 1);
        ItShouldOnlyContainYear(budgets, 2024);
        ItShouldOnlyContainStatus(budgets, "Draft");
    }

    private async Task<Guid> CreateTestBudget(HttpClient client, string name, int year, int month, string currency)
    {
        CreateBudgetRequest request = new(name, year, month, currency);
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/budgets", request, TestContext.Current.CancellationToken);
        
        string content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        BudgetResponse? budgetResponse = JsonSerializer.Deserialize<BudgetResponse>(content, JsonOptions);
        
        return budgetResponse!.Id;
    }

    #region Assertion Helpers

    private static void ItShouldReturnOkStatus(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private static void ItShouldReturnEmptyList(IReadOnlyList<BudgetSummaryResponse>? budgets)
    {
        budgets.ShouldNotBeNull("Response should contain budget list");
        budgets.ShouldBeEmpty("Should return empty list when no budgets exist");
    }

    private static void ItShouldReturnExpectedCount(IReadOnlyList<BudgetSummaryResponse>? budgets, int expectedCount)
    {
        budgets.ShouldNotBeNull("Response should contain budget list");
        budgets.Count.ShouldBe(expectedCount, $"Should return {expectedCount} budgets");
    }

    private static void ItShouldBeOrderedByPeriodDescending(IReadOnlyList<BudgetSummaryResponse>? budgets)
    {
        budgets.ShouldNotBeNull("Response should contain budget list");
        
        for (int i = 1; i < budgets.Count; i++)
        {
            BudgetSummaryResponse previous = budgets[i - 1];
            BudgetSummaryResponse current = budgets[i];
            
            // Compare periods - newer periods should come first
            int previousPeriod = previous.Year * 12 + previous.Month;
            int currentPeriod = current.Year * 12 + current.Month;
            
            previousPeriod.ShouldBeGreaterThanOrEqualTo(currentPeriod, 
                $"Budget at index {i-1} ({previous.Year}-{previous.Month:D2}) should be newer than budget at index {i} ({current.Year}-{current.Month:D2})");
        }
    }

    private static void ItShouldOnlyContainYear(IReadOnlyList<BudgetSummaryResponse>? budgets, int expectedYear)
    {
        budgets.ShouldNotBeNull("Response should contain budget list");
        budgets.ShouldAllBe(b => b.Year == expectedYear, $"All budgets should be for year {expectedYear}");
    }

    private static void ItShouldOnlyContainStatus(IReadOnlyList<BudgetSummaryResponse>? budgets, string expectedStatus)
    {
        budgets.ShouldNotBeNull("Response should contain budget list");
        budgets.ShouldAllBe(b => b.Status == expectedStatus, $"All budgets should have status {expectedStatus}");
    }

    #endregion
}