using Menlo.Api.Budget;
using Menlo.Lib.Common.ValueObjects;
using System.Net;
using System.Text.Json;

namespace Menlo.Api.Tests.Budget;

[ClassDataSource<BudgetApiFixture>(Shared = SharedType.PerTestSession)]
[NotInParallel("Budget")]
public sealed class GetBudgetEndpointTests(BudgetApiFixture fixture) : TestFixture
{
    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNameCaseInsensitive = true };

    // -------------------------------------------------------------------------
    // DB-backed happy-path scenarios
    // -------------------------------------------------------------------------

    [Test]
    public async Task GivenVerifiedBudget_WhenGetBudget_ThenReturns200WithBudgetDto()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory();
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);
        int year = DateTimeOffset.UtcNow.Year;

        HttpResponseMessage createResponse =
            await client.PostAsync($"/api/budgets/{year}", null, TestContext.Current!.Execution.CancellationToken);
        BudgetDto? createdDto = await DeserializeBudgetDto(createResponse);

        createResponse.IsSuccessStatusCode.ShouldBeTrue();
        createdDto.ShouldNotBeNull();

        HttpResponseMessage getResponse =
            await client.GetAsync($"/api/budgets/{createdDto.Id}", TestContext.Current!.Execution.CancellationToken);
        BudgetDto? dto = await DeserializeBudgetDto(getResponse);

        ItShouldHaveReturned200Ok(getResponse);
        ItShouldHaveMatchingId(dto, createdDto.Id);
        ItShouldHaveTheCorrectYear(dto, year);
        ItShouldHaveTheDraftStatus(dto);
        ItShouldHaveZeroTotalPlannedMonthlyAmount(dto);
        ItShouldHaveNoCategories(dto);
    }

    // -------------------------------------------------------------------------
    // Not found scenarios
    // -------------------------------------------------------------------------

    [Test]
    public async Task GivenUnknownBudgetId_WhenGetBudget_ThenReturns404()
    {
        using HttpClient client = fixture.CreateClient();
        Guid unknownId = Guid.NewGuid();

        HttpResponseMessage response =
            await client.GetAsync($"/api/budgets/{unknownId}", TestContext.Current!.Execution.CancellationToken);

        ItShouldHaveReturned404NotFound(response);
    }

    [Test]
    public async Task GivenBudgetCreatedByAnotherMember_WhenGetBudget_ThenReturnsIt()
    {
        await using BudgetTestWebApplicationFactory ownerFactory = fixture.CreateIsolatedFactory();
        using HttpClient ownerClient = await ownerFactory.CreateAntiforgeryClientAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);
        int year = DateTimeOffset.UtcNow.Year;

        HttpResponseMessage createResponse =
            await ownerClient.PostAsync($"/api/budgets/{year}", null, TestContext.Current!.Execution.CancellationToken);
        BudgetDto? createdDto = await DeserializeBudgetDto(createResponse);

        createResponse.IsSuccessStatusCode.ShouldBeTrue();
        createdDto.ShouldNotBeNull();

        // Another signed-in member sees the same budget.
        await using BudgetTestWebApplicationFactory otherFactory = fixture.CreateIsolatedFactory(keepData: true, asDifferentMember: true);
        using HttpClient otherClient = otherFactory.CreateClient();

        HttpResponseMessage getResponse =
            await otherClient.GetAsync($"/api/budgets/{createdDto.Id}", TestContext.Current!.Execution.CancellationToken);
        BudgetDto? fetched = await DeserializeBudgetDto(getResponse);

        ItShouldHaveReturned200Ok(getResponse);
        fetched.ShouldNotBeNull();
        fetched.Id.ShouldBe(createdDto.Id);
    }

    // -------------------------------------------------------------------------
    // Standalone factory tests — no DB needed
    // -------------------------------------------------------------------------

    [Test]
    public async Task GivenUnauthenticatedUser_WhenGetBudget_ThenReturns401()
    {
        await using TestWebApplicationFactory factory = new()
        {
            SimulateUnauthenticated = true,
            MenloConnectionString = null
        };
        using HttpClient client = factory.CreateClient();
        Guid anyId = Guid.NewGuid();

        HttpResponseMessage response =
            await client.GetAsync($"/api/budgets/{anyId}", TestContext.Current!.Execution.CancellationToken);

        ItShouldHaveBeenUnauthorised(response);
    }

    [Test]
    public async Task GivenFeatureToggleOff_WhenGetBudget_ThenReturns404()
    {
        await using TestWebApplicationFactory factory = new()
        {
            ConfigurationOverrides = new Dictionary<string, string?>
            {
                ["Features:Budget"] = "false"
            }
        };
        using HttpClient client = factory.CreateClient();
        Guid anyId = Guid.NewGuid();

        HttpResponseMessage response =
            await client.GetAsync($"/api/budgets/{anyId}", TestContext.Current!.Execution.CancellationToken);

        ItShouldHaveReturned404NotFound(response);
    }

    // -------------------------------------------------------------------------
    // Assertion helpers
    // -------------------------------------------------------------------------

    private static void ItShouldHaveReturned200Ok(HttpResponseMessage response) =>
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

    private static void ItShouldHaveReturned404NotFound(HttpResponseMessage response) =>
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);

    private static void ItShouldHaveMatchingId(BudgetDto? dto, Guid expectedId)
    {
        dto.ShouldNotBeNull();
        dto.Id.ShouldBe(expectedId);
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

    private static async Task<BudgetDto?> DeserializeBudgetDto(HttpResponseMessage response)
    {
        string content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<BudgetDto>(content, JsonOptions);
    }
}
