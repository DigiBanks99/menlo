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

namespace Menlo.Api.Tests.Budgets.Endpoints;

/// <summary>
/// Tests for CreateBudgetEndpoint.
/// </summary>
public sealed class CreateBudgetEndpointTests(TestWebApplicationFactory factory)
    : TestFixture, IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory = factory;

    private static JsonSerializerOptions JsonOptions { get; } = new()
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public async Task GivenValidRequest_WhenCreatingBudget()
    {
        // Arrange
        HttpClient client = _factory.CreateClient();
        CreateBudgetRequest request = new(
            Name: "Monthly Budget",
            Year: 2024,
            Month: 3,
            Currency: "ZAR");

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/budgets",
            request,
            TestContext.Current.CancellationToken);

        // Assert
        string rawContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        response.IsSuccessStatusCode.ShouldBeTrue($"Expected success but got {response.StatusCode}. Response: {rawContent}");

        BudgetResponse? budgetResponse = JsonSerializer.Deserialize<BudgetResponse>(rawContent, JsonOptions);

        ItShouldReturnCreatedStatus(response);
        ItShouldReturnBudgetResponse(budgetResponse);
        ItShouldHaveRequestedName(budgetResponse, "Monthly Budget");
        ItShouldHaveRequestedPeriod(budgetResponse, 2024, 3);
        ItShouldHaveRequestedCurrency(budgetResponse, "ZAR");
        ItShouldHaveDraftStatus(budgetResponse);
        ItShouldHaveEmptyCategories(budgetResponse);
        ItShouldHaveLocationHeader(response);
    }

    [Fact]
    public async Task GivenEmptyName_WhenCreatingBudget()
    {
        // Arrange
        HttpClient client = _factory.CreateClient();
        CreateBudgetRequest request = new(
            Name: "",
            Year: 2024,
            Month: 3,
            Currency: "ZAR");

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/budgets",
            request,
            TestContext.Current.CancellationToken);

        ProblemDetails? problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(
            TestContext.Current.CancellationToken);

        // Assert
        ItShouldHaveBadRequestStatus(response);
        ItShouldReturnProblemDetails(problemDetails);
        ItShouldHaveValidationError(problemDetails);
    }

    [Fact]
    public async Task GivenInvalidYear_WhenCreatingBudget()
    {
        // Arrange
        HttpClient client = _factory.CreateClient();
        CreateBudgetRequest request = new(
            Name: "Test Budget",
            Year: 1800, // Below minimum year
            Month: 3,
            Currency: "ZAR");

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/budgets",
            request,
            TestContext.Current.CancellationToken);

        ProblemDetails? problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(
            TestContext.Current.CancellationToken);

        // Assert
        ItShouldHaveBadRequestStatus(response);
        ItShouldReturnProblemDetails(problemDetails);
        ItShouldHaveInvalidPeriodError(problemDetails);
    }

    [Fact]
    public async Task GivenInvalidMonth_WhenCreatingBudget()
    {
        // Arrange
        HttpClient client = _factory.CreateClient();
        CreateBudgetRequest request = new(
            Name: "Test Budget",
            Year: 2024,
            Month: 15, // Invalid month
            Currency: "ZAR");

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/budgets",
            request,
            TestContext.Current.CancellationToken);

        ProblemDetails? problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(
            TestContext.Current.CancellationToken);

        // Assert
        ItShouldHaveBadRequestStatus(response);
        ItShouldReturnProblemDetails(problemDetails);
        ItShouldHaveInvalidPeriodError(problemDetails);
    }

    [Fact]
    public async Task GivenInvalidCurrency_WhenCreatingBudget()
    {
        // Arrange
        HttpClient client = _factory.CreateClient();
        CreateBudgetRequest request = new(
            Name: "Test Budget",
            Year: 2024,
            Month: 3,
            Currency: "INVALID"); // Invalid currency code

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/budgets",
            request,
            TestContext.Current.CancellationToken);

        ProblemDetails? problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(
            TestContext.Current.CancellationToken);

        // Assert
        ItShouldHaveBadRequestStatus(response);
        ItShouldReturnProblemDetails(problemDetails);
        ItShouldHaveInvalidCurrencyError(problemDetails);
    }

    [Fact]
    public async Task GivenDuplicateBudget_WhenCreatingBudget()
    {
        // Arrange
        HttpClient client = _factory.CreateClient();
        CreateBudgetRequest request = new(
            Name: "Duplicate Budget",
            Year: 2024,
            Month: 6,
            Currency: "USD");

        // Create first budget
        await client.PostAsJsonAsync("/api/budgets", request, TestContext.Current.CancellationToken);

        // Act - Create duplicate
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/budgets",
            request,
            TestContext.Current.CancellationToken);

        ProblemDetails? problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(
            TestContext.Current.CancellationToken);

        // Assert
        ItShouldHaveConflictStatus(response);
        ItShouldReturnProblemDetails(problemDetails);
        ItShouldHaveDuplicateBudgetError(problemDetails);
    }

    [Fact]
    public async Task GivenValidRequestWithDifferentCurrency_WhenCreatingBudget()
    {
        // Arrange
        HttpClient client = _factory.CreateClient();
        CreateBudgetRequest request = new(
            Name: "USD Budget",
            Year: 2024,
            Month: 12,
            Currency: "USD");

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/budgets",
            request,
            TestContext.Current.CancellationToken);

        string rawContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        BudgetResponse? budgetResponse = JsonSerializer.Deserialize<BudgetResponse>(rawContent, JsonOptions);

        // Assert
        ItShouldReturnCreatedStatus(response);
        ItShouldReturnBudgetResponse(budgetResponse);
        ItShouldHaveRequestedCurrency(budgetResponse, "USD");
        ItShouldHaveZeroTotal(budgetResponse, "USD");
    }

    #region Assertion Helpers

    private static void ItShouldReturnCreatedStatus(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    private static void ItShouldHaveBadRequestStatus(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    private static void ItShouldHaveConflictStatus(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    private static void ItShouldReturnBudgetResponse(BudgetResponse? response)
    {
        response.ShouldNotBeNull("Response should contain a budget");
    }

    private static void ItShouldReturnProblemDetails(ProblemDetails? problemDetails)
    {
        problemDetails.ShouldNotBeNull("Response should contain problem details");
    }

    private static void ItShouldHaveRequestedName(BudgetResponse? response, string expectedName)
    {
        response!.Name.ShouldBe(expectedName, "Budget name should match request");
    }

    private static void ItShouldHaveRequestedPeriod(BudgetResponse? response, int expectedYear, int expectedMonth)
    {
        response!.Year.ShouldBe(expectedYear, "Budget year should match request");
        response.Month.ShouldBe(expectedMonth, "Budget month should match request");
    }

    private static void ItShouldHaveRequestedCurrency(BudgetResponse? response, string expectedCurrency)
    {
        response!.Currency.ShouldBe(expectedCurrency, "Budget currency should match request");
        response.Total.Currency.ShouldBe(expectedCurrency, "Total currency should match budget currency");
    }

    private static void ItShouldHaveDraftStatus(BudgetResponse? response)
    {
        response!.Status.ShouldBe("Draft", "New budget should have Draft status");
    }

    private static void ItShouldHaveEmptyCategories(BudgetResponse? response)
    {
        response!.Categories.ShouldBeEmpty("New budget should have no categories");
    }

    private static void ItShouldHaveZeroTotal(BudgetResponse? response, string expectedCurrency)
    {
        response!.Total.Amount.ShouldBe(0.0m, "New budget should have zero total");
        response.Total.Currency.ShouldBe(expectedCurrency, "Total currency should match budget currency");
    }

    private static void ItShouldHaveLocationHeader(HttpResponseMessage response)
    {
        response.Headers.Location.ShouldNotBeNull("Response should include Location header");
    }

    private static void ItShouldHaveValidationError(ProblemDetails? problemDetails)
    {
        problemDetails!.Extensions.ShouldContainKey("errorCode");
        string? errorCode = problemDetails.Extensions["errorCode"]?.ToString();
        errorCode.ShouldNotBeNullOrEmpty("Error code should be present for validation errors");
    }

    private static void ItShouldHaveInvalidPeriodError(ProblemDetails? problemDetails)
    {
        problemDetails!.Extensions.ShouldContainKey("errorCode");
        string? errorCode = problemDetails.Extensions["errorCode"]?.ToString();
        errorCode.ShouldBe("Budget.InvalidPeriod", "Should indicate invalid period error");
    }

    private static void ItShouldHaveInvalidCurrencyError(ProblemDetails? problemDetails)
    {
        problemDetails!.Extensions.ShouldContainKey("errorCode");
        string? errorCode = problemDetails.Extensions["errorCode"]?.ToString();
        errorCode.ShouldBe("Budget.InvalidCurrency", "Should indicate invalid currency error");
    }

    private static void ItShouldHaveDuplicateBudgetError(ProblemDetails? problemDetails)
    {
        problemDetails!.Extensions.ShouldContainKey("errorCode");
        string? errorCode = problemDetails.Extensions["errorCode"]?.ToString();
        errorCode.ShouldBe("Budget.Duplicate", "Should indicate duplicate budget error");
    }

    #endregion
}