using Menlo.Api.Budget.Categories;
using Menlo.Api.Budget.Items;
using Menlo.Api.Budget.Summary;
using Menlo.Lib.Common.ValueObjects;
using Shouldly;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Menlo.Api.Tests.Budget.Summary;

[Collection("Budget")]
public sealed class BudgetSummaryEndpointTests(BudgetApiFixture fixture) : TestFixture
{
    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNameCaseInsensitive = true };

    // Unique household IDs per test scenario — prefix 9 avoids all existing test conflicts.
    private static readonly HouseholdId SummaryAggregateHousehold =
        new(Guid.Parse("90909090-9090-9090-9090-909090909090"));

    private static readonly HouseholdId SummaryMonthFilterHousehold =
        new(Guid.Parse("91919191-9191-9191-9191-919191919191"));

    private static readonly HouseholdId SummaryDeletedExcludedHousehold =
        new(Guid.Parse("92929292-9292-9292-9292-929292929292"));

    private static readonly HouseholdId SummaryUnknownBudgetHousehold =
        new(Guid.Parse("94949494-9494-9494-9494-949494949494"));

    private static readonly HouseholdId SummaryForeignHouseholdOwner =
        new(Guid.Parse("95959595-9595-9595-9595-959595959595"));

    private static readonly HouseholdId SummaryForeignHouseholdAccessor =
        new(Guid.Parse("96969696-9696-9696-9696-969696969696"));

    // =========================================================================
    // POSITIVE AGGREGATE SUMMARY
    // =========================================================================

    [Fact]
    public async Task GivenBudgetWithIncomeAndExpenseItems_WhenGetSummary_ThenReturns200WithCorrectTotals()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(SummaryAggregateHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: TestContext.Current.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);

        // Income: root → child with item 5000
        (_, Guid incomeLeafId) = await CreateRootAndLeafAsync(client, budgetId, "Salary", "Income");
        await CreateItemAsync(client, budgetId, incomeLeafId, "Income", month: 1, amount: 5000m);

        // Expense: root → child with item 1500
        (_, Guid expenseLeafId) = await CreateRootAndLeafAsync(client, budgetId, "Housing", "Expense");
        await CreateItemAsync(client, budgetId, expenseLeafId, "Expense", month: 1, amount: 1500m);

        HttpResponseMessage response = await client.GetAsync(
            $"/api/budgets/{budgetId}/summary", TestContext.Current.CancellationToken);

        BudgetSummaryDto? dto = await DeserializeSummaryDtoAsync(response);

        ItShouldHaveReturned200Ok(response);
        dto.ShouldNotBeNull();
        dto.BudgetId.ShouldBe(budgetId);
        ItShouldHaveIncomeTotal(dto, 5000m);
        ItShouldHaveExpenseTotal(dto, 1500m);
        ItShouldHaveNetPlanned(dto, 3500m); // 5000 - 1500
    }

    // =========================================================================
    // MONTH FILTERING
    // =========================================================================

    [Fact]
    public async Task GivenItemsInMultipleMonths_WhenGetSummaryWithMonthFilter_ThenOnlyThatMonthIncluded()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(SummaryMonthFilterHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: TestContext.Current.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        (_, Guid leafId) = await CreateRootAndLeafAsync(client, budgetId, "Utilities", "Expense");
        await CreateItemAsync(client, budgetId, leafId, "Expense", month: 1, amount: 200m);
        await CreateItemAsync(client, budgetId, leafId, "Expense", month: 2, amount: 300m);

        // Without filter — totals both months
        HttpResponseMessage allResponse = await client.GetAsync(
            $"/api/budgets/{budgetId}/summary", TestContext.Current.CancellationToken);
        BudgetSummaryDto? allDto = await DeserializeSummaryDtoAsync(allResponse);

        // With month=1 filter — only month 1
        HttpResponseMessage m1Response = await client.GetAsync(
            $"/api/budgets/{budgetId}/summary?month=1", TestContext.Current.CancellationToken);
        BudgetSummaryDto? m1Dto = await DeserializeSummaryDtoAsync(m1Response);

        ItShouldHaveReturned200Ok(allResponse);
        ItShouldHaveReturned200Ok(m1Response);

        allDto.ShouldNotBeNull();
        allDto.Month.ShouldBeNull();
        ItShouldHaveExpenseTotal(allDto, 500m); // 200 + 300

        m1Dto.ShouldNotBeNull();
        m1Dto.Month.ShouldBe(1);
        ItShouldHaveExpenseTotal(m1Dto, 200m); // only month 1
    }

    // =========================================================================
    // DELETED ITEMS EXCLUDED
    // =========================================================================

    [Fact]
    public async Task GivenDeletedItem_WhenGetSummary_ThenDeletedItemExcludedFromTotals()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(SummaryDeletedExcludedHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: TestContext.Current.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        (_, Guid leafId) = await CreateRootAndLeafAsync(client, budgetId, "Groceries", "Expense");

        // Create two items; delete one
        Guid item1Id = await CreateItemAsync(client, budgetId, leafId, "Expense", month: 1, amount: 1000m);
        await CreateItemAsync(client, budgetId, leafId, "Expense", month: 2, amount: 500m);

        HttpResponseMessage deleteResponse = await client.DeleteAsync(
            $"/api/budgets/{budgetId}/categories/{leafId}/items/{item1Id}",
            TestContext.Current.CancellationToken);
        deleteResponse.IsSuccessStatusCode.ShouldBeTrue(
            $"Item deletion failed: {await deleteResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)}");

        HttpResponseMessage response = await client.GetAsync(
            $"/api/budgets/{budgetId}/summary", TestContext.Current.CancellationToken);

        BudgetSummaryDto? dto = await DeserializeSummaryDtoAsync(response);

        ItShouldHaveReturned200Ok(response);
        dto.ShouldNotBeNull();
        // Only month-2 item (500) should count; month-1 item (1000) was deleted
        ItShouldHaveExpenseTotal(dto, 500m);
    }

    // =========================================================================
    // FEATURE FLAG OFF
    // =========================================================================

    [Fact]
    public async Task GivenFeatureToggleOff_WhenGetSummary_ThenReturns404()
    {
        await using TestWebApplicationFactory factory = new()
        {
            ConfigurationOverrides = new Dictionary<string, string?>
            {
                ["Features:Budget"] = "true",
                ["Features:BudgetItems"] = "false"
            }
        };
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            $"/api/budgets/{Guid.NewGuid()}/summary", TestContext.Current.CancellationToken);

        ItShouldHaveReturned404NotFound(response);
    }

    // =========================================================================
    // NOT FOUND
    // =========================================================================

    [Fact]
    public async Task GivenUnknownBudgetId_WhenGetSummary_ThenReturns404()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(SummaryUnknownBudgetHousehold);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            $"/api/budgets/{Guid.NewGuid()}/summary", TestContext.Current.CancellationToken);

        ItShouldHaveReturned404NotFound(response);
    }

    [Fact]
    public async Task GivenBudgetOwnedByOtherHousehold_WhenGetSummary_ThenReturns404()
    {
        // Create a budget under a dedicated owner household
        await using BudgetTestWebApplicationFactory ownerFactory = CreateIsolatedFactory(SummaryForeignHouseholdOwner);
        using HttpClient ownerClient = await ownerFactory.CreateAntiforgeryClientAsync(cancellationToken: TestContext.Current.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(ownerClient);

        // Try to access it from a completely different household
        await using BudgetTestWebApplicationFactory accessorFactory = CreateIsolatedFactory(SummaryForeignHouseholdAccessor);
        using HttpClient accessorClient = accessorFactory.CreateClient();

        HttpResponseMessage response = await accessorClient.GetAsync(
            $"/api/budgets/{budgetId}/summary", TestContext.Current.CancellationToken);

        ItShouldHaveReturned404NotFound(response);
    }

    // =========================================================================
    // UNAUTHENTICATED
    // =========================================================================

    [Fact]
    public async Task GivenUnauthenticatedUser_WhenGetSummary_ThenReturns401()
    {
        await using TestWebApplicationFactory factory = new()
        {
            SimulateUnauthenticated = true,
            MenloConnectionString = null
        };
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            $"/api/budgets/{Guid.NewGuid()}/summary", TestContext.Current.CancellationToken);

        ItShouldHaveBeenUnauthorised(response);
    }

    // =========================================================================
    // FACTORY HELPERS
    // =========================================================================

    private BudgetTestWebApplicationFactory CreateIsolatedFactory(HouseholdId householdId) =>
        new(householdId)
        {
            MenloConnectionString = fixture.ConnectionString,
            SkipMigration = true,
            ConfigurationOverrides = new Dictionary<string, string?>
            {
                ["Features:Budget"] = "true",
                ["Features:BudgetItems"] = "true"
            }
        };

    // =========================================================================
    // DATA SETUP HELPERS
    // =========================================================================

    private static async Task<Guid> CreateBudgetAsync(HttpClient client)
    {
        int year = DateTimeOffset.UtcNow.Year;
        HttpResponseMessage response = await client.PostAsync(
            $"/api/budgets/{year}", null, TestContext.Current.CancellationToken);
        response.IsSuccessStatusCode.ShouldBeTrue(
            $"Budget creation failed: {await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)}");

        string content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        return JsonDocument.Parse(content).RootElement.GetProperty("id").GetGuid();
    }

    /// <summary>
    /// Creates a root category then a leaf child under it; returns both IDs.
    /// </summary>
    private static async Task<(Guid RootId, Guid LeafId)> CreateRootAndLeafAsync(
        HttpClient client, Guid budgetId, string name, string budgetFlow)
    {
        string suffix = Guid.NewGuid().ToString("N")[..6];

        HttpResponseMessage rootResponse = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories",
            new CreateCategoryRequest($"{name}-{suffix}", budgetFlow),
            TestContext.Current.CancellationToken);
        rootResponse.IsSuccessStatusCode.ShouldBeTrue(
            $"Root category creation failed: {await rootResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)}");
        string rootContent = await rootResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Guid rootId = JsonDocument.Parse(rootContent).RootElement.GetProperty("id").GetGuid();

        HttpResponseMessage leafResponse = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories",
            new CreateCategoryRequest($"{name}-leaf-{suffix}", budgetFlow, ParentId: rootId),
            TestContext.Current.CancellationToken);
        leafResponse.IsSuccessStatusCode.ShouldBeTrue(
            $"Leaf category creation failed: {await leafResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)}");
        string leafContent = await leafResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Guid leafId = JsonDocument.Parse(leafContent).RootElement.GetProperty("id").GetGuid();

        return (rootId, leafId);
    }

    /// <summary>
    /// Creates a budget item and returns its ID.
    /// </summary>
    private static async Task<Guid> CreateItemAsync(
        HttpClient client, Guid budgetId, Guid categoryId, string budgetFlow, int month, decimal amount)
    {
        CreateBudgetItemRequest request = new(
            Month: month,
            BudgetFlow: budgetFlow,
            PlannedAmount: amount,
            PlannedCurrency: "ZAR",
            PayerSplit: [new PayerAllocationDto(Guid.NewGuid(), 100)],
            AttributionSplit: [new AttributionAllocationDto("Main", 100)]);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items",
            request,
            TestContext.Current.CancellationToken);
        response.IsSuccessStatusCode.ShouldBeTrue(
            $"Item creation failed: {await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)}");

        string content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        return JsonDocument.Parse(content).RootElement.GetProperty("id").GetGuid();
    }

    // =========================================================================
    // DESERIALIZATION HELPERS
    // =========================================================================

    private static async Task<BudgetSummaryDto?> DeserializeSummaryDtoAsync(HttpResponseMessage response)
    {
        string content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        return JsonSerializer.Deserialize<BudgetSummaryDto>(content, JsonOptions);
    }

    // =========================================================================
    // ASSERTION HELPERS
    // =========================================================================

    private static void ItShouldHaveReturned200Ok(HttpResponseMessage response) =>
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

    private static void ItShouldHaveReturned404NotFound(HttpResponseMessage response) =>
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);

    private static void ItShouldHaveIncomeTotal(BudgetSummaryDto dto, decimal expected)
    {
        decimal total = dto.Income.Sum(c => c.PlannedTotal);
        total.ShouldBe(expected, $"Expected income total {expected} but got {total}");
    }

    private static void ItShouldHaveExpenseTotal(BudgetSummaryDto dto, decimal expected)
    {
        decimal total = dto.Expenses.Sum(c => c.PlannedTotal);
        total.ShouldBe(expected, $"Expected expense total {expected} but got {total}");
    }

    private static void ItShouldHaveNetPlanned(BudgetSummaryDto dto, decimal expected) =>
        dto.NetPlanned.ShouldBe(expected, $"Expected NetPlanned {expected} but got {dto.NetPlanned}");
}
