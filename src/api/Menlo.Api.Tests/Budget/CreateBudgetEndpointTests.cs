using CSharpFunctionalExtensions;
using Menlo.Api.Budget;
using Menlo.Lib.Auth.Abstractions;
using Menlo.Lib.Auth.Errors;
using Menlo.Lib.Auth.Models;
using Menlo.Lib.Common.ValueObjects;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Menlo.Api.Tests.Budget;

[ClassDataSource<BudgetApiFixture>(Shared = SharedType.PerTestSession)]
[NotInParallel("Budget")]
public sealed class CreateBudgetEndpointTests(BudgetApiFixture fixture) : TestFixture
{
    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNameCaseInsensitive = true };

    // -------------------------------------------------------------------------
    // DB-backed happy-path scenarios (use the shared fixture factory)
    // -------------------------------------------------------------------------

    [Test]
    public async Task GivenValidYear_WhenPostBudget_ThenReturns201WithBudgetDto()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory();
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);
        int year = DateTimeOffset.UtcNow.Year;

        HttpResponseMessage response =
            await client.PostAsync($"/api/budgets/{year}", null, TestContext.Current!.Execution.CancellationToken);

        BudgetDto? dto = await DeserializeBudgetDto(response);

        ItShouldHaveReturned201Created(response);
        ItShouldHaveANonEmptyId(dto);
        ItShouldHaveTheCorrectYear(dto, year);
        ItShouldHaveTheDraftStatus(dto);
        ItShouldBelongToTheTestHousehold(dto, factory.HouseholdId);
        ItShouldHaveZeroTotalPlannedMonthlyAmount(dto);
        ItShouldHaveNoCategories(dto);
    }

    [Test]
    public async Task GivenExistingBudget_WhenPostBudgetAgain_ThenReturns200WithSameBudgetId()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory();
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);
        int year = DateTimeOffset.UtcNow.Year;

        HttpResponseMessage firstResponse =
            await client.PostAsync($"/api/budgets/{year}", null, TestContext.Current!.Execution.CancellationToken);
        BudgetDto? firstDto = await DeserializeBudgetDto(firstResponse);

        HttpResponseMessage secondResponse =
            await client.PostAsync($"/api/budgets/{year}", null, TestContext.Current!.Execution.CancellationToken);
        BudgetDto? secondDto = await DeserializeBudgetDto(secondResponse);

        ItShouldHaveReturned200Ok(secondResponse);
        ItShouldReturnTheSameBudgetId(firstDto, secondDto);
    }

    [Test]
    public async Task GivenNextYear_WhenPostBudget_ThenReturns201()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory();
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);
        int year = DateTimeOffset.UtcNow.Year + 1;

        HttpResponseMessage response =
            await client.PostAsync($"/api/budgets/{year}", null, TestContext.Current!.Execution.CancellationToken);

        BudgetDto? dto = await DeserializeBudgetDto(response);

        ItShouldHaveReturned201Created(response);
        ItShouldHaveTheCorrectYear(dto, year);
    }

    // -------------------------------------------------------------------------
    // Year validation (use the shared fixture factory — feature enabled + user mocked)
    // -------------------------------------------------------------------------

    [Test]
    public async Task GivenPastYear_WhenPostBudget_ThenReturns400()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory();
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);
        int pastYear = DateTimeOffset.UtcNow.Year - 1;

        HttpResponseMessage response =
            await client.PostAsync($"/api/budgets/{pastYear}", null, TestContext.Current!.Execution.CancellationToken);

        ItShouldHaveReturned400BadRequest(response);
    }

    [Test]
    public async Task GivenFutureYearBeyondNextYear_WhenPostBudget_ThenReturns400()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory();
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);
        int farFutureYear = DateTimeOffset.UtcNow.Year + 2;

        HttpResponseMessage response =
            await client.PostAsync($"/api/budgets/{farFutureYear}", null, TestContext.Current!.Execution.CancellationToken);

        ItShouldHaveReturned400BadRequest(response);
    }

    // -------------------------------------------------------------------------
    // Standalone factory tests — no DB needed
    // -------------------------------------------------------------------------

    [Test]
    public async Task GivenUnauthenticatedUser_WhenPostBudget_ThenReturns401()
    {
        await using TestWebApplicationFactory factory = new()
        {
            SimulateUnauthenticated = true,
            MenloConnectionString = null
        };
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);
        int year = DateTimeOffset.UtcNow.Year;

        HttpResponseMessage response =
            await client.PostAsync($"/api/budgets/{year}", null, TestContext.Current!.Execution.CancellationToken);

        ItShouldHaveBeenUnauthorised(response);
    }

    [Test]
    public async Task GivenFeatureToggleOff_WhenPostBudget_ThenReturns404()
    {
        await using TestWebApplicationFactory factory = new()
        {
            ConfigurationOverrides = new Dictionary<string, string?>
            {
                ["Features:Budget"] = "false"
            }
        };
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);
        int year = DateTimeOffset.UtcNow.Year;

        HttpResponseMessage response =
            await client.PostAsync($"/api/budgets/{year}", null, TestContext.Current!.Execution.CancellationToken);

        ItShouldHaveReturned404NotFound(response);
    }

    [Test]
    public async Task GivenDevelopmentEnvironmentWithoutOverrides_WhenPostBudget_ThenFeatureIsEnabledByDefault()
    {
        await using TestWebApplicationFactory factory = new()
        {
            EnvironmentName = Environments.Development,
            SimulateUnauthenticated = true,
            ConfigurationOverrides = new Dictionary<string, string?>
            {
                ["Services:web-ui:http:0"] = "http://localhost:5173"
            }
        };
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);
        int year = DateTimeOffset.UtcNow.Year;

        HttpResponseMessage response =
            await client.PostAsync($"/api/budgets/{year}", null, TestContext.Current!.Execution.CancellationToken);

        ItShouldHaveBeenUnauthorised(response);
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

}
