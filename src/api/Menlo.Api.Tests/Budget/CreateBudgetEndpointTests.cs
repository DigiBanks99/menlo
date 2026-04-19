using CSharpFunctionalExtensions;
using Menlo.Api.Budget;
using Menlo.Lib.Auth.Abstractions;
using Menlo.Lib.Auth.Errors;
using Menlo.Lib.Auth.Models;
using Menlo.Lib.Common.ValueObjects;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Menlo.Api.Tests.Budget;

[Collection("Budget")]
public sealed class CreateBudgetEndpointTests(BudgetApiFixture fixture) : TestFixture
{
    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNameCaseInsensitive = true };

    private static readonly HouseholdId CreateBudgetHousehold =
        new(Guid.Parse("b0b0b0b0-b0b0-b0b0-b0b0-b0b0b0b0b0b0"));

    private static readonly HouseholdId ExistingBudgetHousehold =
        new(Guid.Parse("b1b1b1b1-b1b1-b1b1-b1b1-b1b1b1b1b1b1"));

    private static readonly HouseholdId NextYearBudgetHousehold =
        new(Guid.Parse("b2b2b2b2-b2b2-b2b2-b2b2-b2b2b2b2b2b2"));

    private static readonly HouseholdId PastYearBudgetHousehold =
        new(Guid.Parse("b3b3b3b3-b3b3-b3b3-b3b3-b3b3b3b3b3b3"));

    private static readonly HouseholdId FutureYearBudgetHousehold =
        new(Guid.Parse("b4b4b4b4-b4b4-b4b4-b4b4-b4b4b4b4b4b4"));

    // -------------------------------------------------------------------------
    // DB-backed happy-path scenarios (use the shared fixture factory)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GivenValidYear_WhenPostBudget_ThenReturns201WithBudgetDto()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(CreateBudgetHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: TestContext.Current.CancellationToken);
        int year = DateTimeOffset.UtcNow.Year;

        HttpResponseMessage response =
            await client.PostAsync($"/api/budgets/{year}", null, TestContext.Current.CancellationToken);

        BudgetDto? dto = await DeserializeBudgetDto(response);

        ItShouldHaveReturned201Created(response);
        ItShouldHaveANonEmptyId(dto);
        ItShouldHaveTheCorrectYear(dto, year);
        ItShouldHaveTheDraftStatus(dto);
        ItShouldBelongToTheTestHousehold(dto, CreateBudgetHousehold);
        ItShouldHaveZeroTotalPlannedMonthlyAmount(dto);
        ItShouldHaveNoCategories(dto);
    }

    [Fact]
    public async Task GivenExistingBudget_WhenPostBudgetAgain_ThenReturns200WithSameBudgetId()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(ExistingBudgetHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: TestContext.Current.CancellationToken);
        int year = DateTimeOffset.UtcNow.Year;

        HttpResponseMessage firstResponse =
            await client.PostAsync($"/api/budgets/{year}", null, TestContext.Current.CancellationToken);
        BudgetDto? firstDto = await DeserializeBudgetDto(firstResponse);

        HttpResponseMessage secondResponse =
            await client.PostAsync($"/api/budgets/{year}", null, TestContext.Current.CancellationToken);
        BudgetDto? secondDto = await DeserializeBudgetDto(secondResponse);

        ItShouldHaveReturned200Ok(secondResponse);
        ItShouldReturnTheSameBudgetId(firstDto, secondDto);
    }

    [Fact]
    public async Task GivenNextYear_WhenPostBudget_ThenReturns201()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(NextYearBudgetHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: TestContext.Current.CancellationToken);
        int year = DateTimeOffset.UtcNow.Year + 1;

        HttpResponseMessage response =
            await client.PostAsync($"/api/budgets/{year}", null, TestContext.Current.CancellationToken);

        BudgetDto? dto = await DeserializeBudgetDto(response);

        ItShouldHaveReturned201Created(response);
        ItShouldHaveTheCorrectYear(dto, year);
    }

    // -------------------------------------------------------------------------
    // Year validation (use the shared fixture factory — feature enabled + user mocked)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GivenPastYear_WhenPostBudget_ThenReturns400()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(PastYearBudgetHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: TestContext.Current.CancellationToken);
        int pastYear = DateTimeOffset.UtcNow.Year - 1;

        HttpResponseMessage response =
            await client.PostAsync($"/api/budgets/{pastYear}", null, TestContext.Current.CancellationToken);

        ItShouldHaveReturned400BadRequest(response);
    }

    [Fact]
    public async Task GivenFutureYearBeyondNextYear_WhenPostBudget_ThenReturns400()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(FutureYearBudgetHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: TestContext.Current.CancellationToken);
        int farFutureYear = DateTimeOffset.UtcNow.Year + 2;

        HttpResponseMessage response =
            await client.PostAsync($"/api/budgets/{farFutureYear}", null, TestContext.Current.CancellationToken);

        ItShouldHaveReturned400BadRequest(response);
    }

    // -------------------------------------------------------------------------
    // Standalone factory tests — no DB needed
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GivenUnauthenticatedUser_WhenPostBudget_ThenReturns401()
    {
        await using TestWebApplicationFactory factory = new()
        {
            SimulateUnauthenticated = true,
            MenloConnectionString = null
        };
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: TestContext.Current.CancellationToken);
        int year = DateTimeOffset.UtcNow.Year;

        HttpResponseMessage response =
            await client.PostAsync($"/api/budgets/{year}", null, TestContext.Current.CancellationToken);

        ItShouldHaveBeenUnauthorised(response);
    }

    [Fact]
    public async Task GivenFeatureToggleOff_WhenPostBudget_ThenReturns404()
    {
        await using TestWebApplicationFactory factory = new()
        {
            ConfigurationOverrides = new Dictionary<string, string?>
            {
                ["Features:Budget"] = "false"
            }
        };
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: TestContext.Current.CancellationToken);
        int year = DateTimeOffset.UtcNow.Year;

        HttpResponseMessage response =
            await client.PostAsync($"/api/budgets/{year}", null, TestContext.Current.CancellationToken);

        ItShouldHaveReturned404NotFound(response);
    }

    // -------------------------------------------------------------------------
    // Assertion helpers
    // -------------------------------------------------------------------------

    private static void ItShouldHaveReturned201Created(HttpResponseMessage response) =>
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

    private static void ItShouldHaveReturned200Ok(HttpResponseMessage response) =>
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

    private static void ItShouldHaveReturned400BadRequest(HttpResponseMessage response) =>
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

    private static void ItShouldHaveReturned404NotFound(HttpResponseMessage response) =>
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);

    private static void ItShouldHaveANonEmptyId(BudgetDto? dto)
    {
        dto.ShouldNotBeNull();
        dto.Id.ShouldNotBe(Guid.Empty);
    }

    private static void ItShouldHaveTheCorrectYear(BudgetDto? dto, int expectedYear)
    {
        dto.ShouldNotBeNull();
        dto.Year.ShouldBe(expectedYear);
    }

    private static void ItShouldHaveTheDraftStatus(BudgetDto? dto)
    {
        dto.ShouldNotBeNull();
        dto.Status.ShouldBe("Draft");
    }

    private static void ItShouldBelongToTheTestHousehold(BudgetDto? dto, HouseholdId expectedHouseholdId)
    {
        dto.ShouldNotBeNull();
        dto.HouseholdId.ShouldBe(expectedHouseholdId.Value);
    }

    private static void ItShouldHaveZeroTotalPlannedMonthlyAmount(BudgetDto? dto)
    {
        dto.ShouldNotBeNull();
        dto.TotalPlannedMonthlyAmount.Amount.ShouldBe(0);
        dto.TotalPlannedMonthlyAmount.Currency.ShouldBe("ZAR");
    }

    private static void ItShouldHaveNoCategories(BudgetDto? dto)
    {
        dto.ShouldNotBeNull();
        dto.Categories.ShouldBeEmpty();
    }

    private static void ItShouldReturnTheSameBudgetId(BudgetDto? firstDto, BudgetDto? secondDto)
    {
        firstDto.ShouldNotBeNull();
        secondDto.ShouldNotBeNull();
        secondDto.Id.ShouldBe(firstDto.Id);
    }

    private static async Task<BudgetDto?> DeserializeBudgetDto(HttpResponseMessage response)
    {
        string content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<BudgetDto>(content, JsonOptions);
    }

    private BudgetTestWebApplicationFactory CreateIsolatedFactory(HouseholdId householdId) =>
        new(householdId)
        {
            MenloConnectionString = fixture.ConnectionString,
            SkipMigration = true,
            ConfigurationOverrides = new Dictionary<string, string?>
            {
                ["Features:Budget"] = "true"
            }
        };
}
