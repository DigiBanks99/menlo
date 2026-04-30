using Menlo.Api.Budget.Categories;
using Menlo.Api.Budget.Items;
using Menlo.Lib.Common.ValueObjects;
using Shouldly;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Menlo.Api.Tests.Budget.Items;

[Collection("Budget")]
public sealed class BudgetItemEndpointTests(BudgetApiFixture fixture) : TestFixture
{
    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNameCaseInsensitive = true };

    // Unique household IDs per test scenario
    private static readonly HouseholdId CreateItemHousehold =
        new(Guid.Parse("d1d1d1d1-d1d1-d1d1-d1d1-d1d1d1d1d1d1"));

    private static readonly HouseholdId InvalidMonthHousehold =
        new(Guid.Parse("d2d2d2d2-d2d2-d2d2-d2d2-d2d2d2d2d2d2"));

    private static readonly HouseholdId NonLeafHousehold =
        new(Guid.Parse("d3d3d3d3-d3d3-d3d3-d3d3-d3d3d3d3d3d3"));

    private static readonly HouseholdId InvalidFlowHousehold =
        new(Guid.Parse("d4d4d4d4-d4d4-d4d4-d4d4-d4d4d4d4d4d4"));

    private static readonly HouseholdId DuplicateItemHousehold =
        new(Guid.Parse("d5d5d5d5-d5d5-d5d5-d5d5-d5d5d5d5d5d5"));

    private static readonly HouseholdId InvalidSplitHousehold =
        new(Guid.Parse("d6d6d6d6-d6d6-d6d6-d6d6-d6d6d6d6d6d6"));

    private static readonly HouseholdId ListItemsHousehold =
        new(Guid.Parse("d7d7d7d7-d7d7-d7d7-d7d7-d7d7d7d7d7d7"));

    private static readonly HouseholdId FeatureOffHousehold =
        new(Guid.Parse("d8d8d8d8-d8d8-d8d8-d8d8-d8d8d8d8d8d8"));

    private static readonly HouseholdId NotFoundBudgetHousehold =
        new(Guid.Parse("d9d9d9d9-d9d9-d9d9-d9d9-d9d9d9d9d9d9"));

    private static readonly HouseholdId ListItemsFilterHousehold =
        new(Guid.Parse("dadadada-dada-dada-dada-dadadadadada"));

    // =========================================================================
    // CREATE BUDGET ITEM
    // =========================================================================

    [Fact]
    public async Task GivenValidRequest_WhenCreateBudgetItem_ThenReturns201WithDto()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(CreateItemHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        Guid categoryId = await CreateLeafCategoryAsync(client, budgetId);
        CreateBudgetItemRequest request = CreateValidItemRequest();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items",
            request,
            TestContext.Current.CancellationToken);

        BudgetItemDto? dto = await DeserializeBudgetItemDto(response);

        ItShouldHaveReturned201Created(response);
        ItShouldHaveANonEmptyId(dto);
        ItShouldBelongToBudget(dto, budgetId);
        ItShouldBelongToCategory(dto, categoryId);
        ItShouldHaveMonth(dto, 1);
        ItShouldHaveBudgetFlow(dto, "Expense");
        ItShouldHavePlannedAmount(dto, 1500.00m, "ZAR");
        ItShouldHavePayerSplitCount(dto, 1);
        ItShouldHaveAttributionSplitCount(dto, 1);
    }

    [Fact]
    public async Task GivenInvalidMonth_WhenCreateBudgetItem_ThenReturns400()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(InvalidMonthHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        Guid categoryId = await CreateLeafCategoryAsync(client, budgetId);
        CreateBudgetItemRequest request = CreateValidItemRequest(month: 0);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items",
            request,
            TestContext.Current.CancellationToken);

        ItShouldHaveReturned400BadRequest(response);
    }

    [Fact]
    public async Task GivenNonLeafCategory_WhenCreateBudgetItem_ThenReturns400()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(NonLeafHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        Guid rootCategoryId = await CreateRootWithChildCategoryAsync(client, budgetId);
        CreateBudgetItemRequest request = CreateValidItemRequest();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{rootCategoryId}/items",
            request,
            TestContext.Current.CancellationToken);

        ItShouldHaveReturned400BadRequest(response);
    }

    [Fact]
    public async Task GivenIncomeCategoryWithExpenseItem_WhenCreate_ThenReturns400()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(InvalidFlowHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        Guid categoryId = await CreateLeafCategoryAsync(client, budgetId, budgetFlow: "Income");

        // Try to add an Expense item to an Income category
        CreateBudgetItemRequest request = CreateValidItemRequest();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items",
            request,
            TestContext.Current.CancellationToken);

        ItShouldHaveReturned400BadRequest(response);
    }

    [Fact]
    public async Task GivenExistingItemForMonth_WhenCreateDuplicate_ThenReturns409()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(DuplicateItemHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        Guid categoryId = await CreateLeafCategoryAsync(client, budgetId);
        CreateBudgetItemRequest request = CreateValidItemRequest(month: 1);

        // First creation should succeed
        HttpResponseMessage firstResponse = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items",
            request,
            TestContext.Current.CancellationToken);
        firstResponse.IsSuccessStatusCode.ShouldBeTrue(
            $"First item creation failed: {await firstResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)}");

        // Duplicate should fail
        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items",
            request,
            TestContext.Current.CancellationToken);

        ItShouldHaveReturned409Conflict(response);
    }

    [Fact]
    public async Task GivenInvalidPayerSplit_WhenCreate_ThenReturns400()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(InvalidSplitHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        Guid categoryId = await CreateLeafCategoryAsync(client, budgetId);

        // Payer split that doesn't sum to 100%
        CreateBudgetItemRequest request = new(
            Month: 1,
            BudgetFlow: "Expense",
            PlannedAmount: 1500.00m,
            PlannedCurrency: "ZAR",
            PayerSplit: [new PayerAllocationDto(Guid.NewGuid(), 60), new PayerAllocationDto(Guid.NewGuid(), 30)],
            AttributionSplit: [new AttributionAllocationDto("Main", 100)]);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items",
            request,
            TestContext.Current.CancellationToken);

        ItShouldHaveReturned400BadRequest(response);
    }

    // =========================================================================
    // LIST BUDGET ITEMS
    // =========================================================================

    [Fact]
    public async Task GivenMultipleItems_WhenListItems_ThenReturnsAllItems()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(ListItemsHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        Guid categoryId = await CreateLeafCategoryAsync(client, budgetId);

        // Create items for months 1, 2, 3
        foreach (int month in new[] { 1, 2, 3 })
        {
            HttpResponseMessage createResponse = await client.PostAsJsonAsync(
                $"/api/budgets/{budgetId}/categories/{categoryId}/items",
                CreateValidItemRequest(month: month),
                TestContext.Current.CancellationToken);
            createResponse.IsSuccessStatusCode.ShouldBeTrue(
                $"Item creation for month {month} failed: {await createResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)}");
        }

        HttpResponseMessage response = await client.GetAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items",
            TestContext.Current.CancellationToken);

        List<BudgetItemDto>? items = await DeserializeBudgetItemList(response);

        ItShouldHaveReturned200Ok(response);
        ItShouldHaveItemCount(items, 3);
        ItShouldContainItemForMonth(items, 1);
        ItShouldContainItemForMonth(items, 2);
        ItShouldContainItemForMonth(items, 3);
    }

    [Fact]
    public async Task GivenMultipleItems_WhenListWithMonthFilter_ThenReturnsFilteredItems()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(ListItemsFilterHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        Guid categoryId = await CreateLeafCategoryAsync(client, budgetId);

        // Create items for months 1, 2, 3
        foreach (int month in new[] { 1, 2, 3 })
        {
            HttpResponseMessage createResponse = await client.PostAsJsonAsync(
                $"/api/budgets/{budgetId}/categories/{categoryId}/items",
                CreateValidItemRequest(month: month),
                TestContext.Current.CancellationToken);
            createResponse.IsSuccessStatusCode.ShouldBeTrue(
                $"Item creation for month {month} failed: {await createResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)}");
        }

        HttpResponseMessage response = await client.GetAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items?month=2",
            TestContext.Current.CancellationToken);

        List<BudgetItemDto>? items = await DeserializeBudgetItemList(response);

        ItShouldHaveReturned200Ok(response);
        ItShouldHaveItemCount(items, 1);
        ItShouldContainItemForMonth(items, 2);
    }

    // =========================================================================
    // FEATURE FLAG & NOT FOUND
    // =========================================================================

    [Fact]
    public async Task GivenFeatureFlagOff_WhenCreateBudgetItem_ThenReturns404()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactoryWithoutBudgetItemsFeature(FeatureOffHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);

        CreateBudgetItemRequest request = CreateValidItemRequest();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{Guid.NewGuid()}/items",
            request,
            TestContext.Current.CancellationToken);

        ItShouldHaveReturned404NotFound(response);
    }

    [Fact]
    public async Task GivenNonExistentBudget_WhenCreateBudgetItem_ThenReturns404()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(NotFoundBudgetHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        CreateBudgetItemRequest request = CreateValidItemRequest();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/budgets/{Guid.NewGuid()}/categories/{Guid.NewGuid()}/items",
            request,
            TestContext.Current.CancellationToken);

        ItShouldHaveReturned404NotFound(response);
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

    private BudgetTestWebApplicationFactory CreateIsolatedFactoryWithoutBudgetItemsFeature(HouseholdId householdId) =>
        new(householdId)
        {
            MenloConnectionString = fixture.ConnectionString,
            SkipMigration = true,
            ConfigurationOverrides = new Dictionary<string, string?>
            {
                ["Features:Budget"] = "true"
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
        JsonDocument doc = JsonDocument.Parse(content);
        return doc.RootElement.GetProperty("id").GetGuid();
    }

    private static async Task<Guid> CreateLeafCategoryAsync(
        HttpClient client, Guid budgetId, string budgetFlow = "Expense")
    {
        // Create root category
        var rootRequest = new CreateCategoryRequest("Expenses", budgetFlow);
        HttpResponseMessage rootResponse = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories", rootRequest, TestContext.Current.CancellationToken);
        rootResponse.IsSuccessStatusCode.ShouldBeTrue(
            $"Root category creation failed: {await rootResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)}");
        string rootContent = await rootResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Guid rootId = JsonDocument.Parse(rootContent).RootElement.GetProperty("id").GetGuid();

        // Create child (leaf) category
        var childRequest = new CreateCategoryRequest("Electricity", budgetFlow, ParentId: rootId);
        HttpResponseMessage childResponse = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories", childRequest, TestContext.Current.CancellationToken);
        childResponse.IsSuccessStatusCode.ShouldBeTrue(
            $"Child category creation failed: {await childResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)}");
        string childContent = await childResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        return JsonDocument.Parse(childContent).RootElement.GetProperty("id").GetGuid();
    }

    /// <summary>
    /// Creates a root category with a child and returns the ROOT category ID (non-leaf).
    /// </summary>
    private static async Task<Guid> CreateRootWithChildCategoryAsync(HttpClient client, Guid budgetId)
    {
        var rootRequest = new CreateCategoryRequest("Housing", "Expense");
        HttpResponseMessage rootResponse = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories", rootRequest, TestContext.Current.CancellationToken);
        rootResponse.IsSuccessStatusCode.ShouldBeTrue(
            $"Root category creation failed: {await rootResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)}");
        string rootContent = await rootResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Guid rootId = JsonDocument.Parse(rootContent).RootElement.GetProperty("id").GetGuid();

        var childRequest = new CreateCategoryRequest("Rent", "Expense", ParentId: rootId);
        HttpResponseMessage childResponse = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories", childRequest, TestContext.Current.CancellationToken);
        childResponse.IsSuccessStatusCode.ShouldBeTrue(
            $"Child category creation failed: {await childResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)}");

        return rootId;
    }

    private static CreateBudgetItemRequest CreateValidItemRequest(int month = 1) =>
        new(
            Month: month,
            BudgetFlow: "Expense",
            PlannedAmount: 1500.00m,
            PlannedCurrency: "ZAR",
            PayerSplit: [new PayerAllocationDto(Guid.NewGuid(), 100)],
            AttributionSplit: [new AttributionAllocationDto("Main", 100)]);

    // =========================================================================
    // DESERIALIZATION HELPERS
    // =========================================================================

    private static async Task<BudgetItemDto?> DeserializeBudgetItemDto(HttpResponseMessage response)
    {
        string content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        return JsonSerializer.Deserialize<BudgetItemDto>(content, JsonOptions);
    }

    private static async Task<List<BudgetItemDto>?> DeserializeBudgetItemList(HttpResponseMessage response)
    {
        string content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        return JsonSerializer.Deserialize<List<BudgetItemDto>>(content, JsonOptions);
    }

    // =========================================================================
    // ASSERTION HELPERS
    // =========================================================================

    private static void ItShouldHaveReturned201Created(HttpResponseMessage response) =>
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

    private static void ItShouldHaveReturned200Ok(HttpResponseMessage response) =>
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

    private static void ItShouldHaveReturned400BadRequest(HttpResponseMessage response) =>
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

    private static void ItShouldHaveReturned404NotFound(HttpResponseMessage response) =>
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);

    private static void ItShouldHaveReturned409Conflict(HttpResponseMessage response) =>
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);

    private static void ItShouldHaveANonEmptyId(BudgetItemDto? dto)
    {
        dto.ShouldNotBeNull();
        dto.Id.ShouldNotBe(Guid.Empty);
    }

    private static void ItShouldBelongToBudget(BudgetItemDto? dto, Guid budgetId)
    {
        dto.ShouldNotBeNull();
        dto.BudgetId.ShouldBe(budgetId);
    }

    private static void ItShouldBelongToCategory(BudgetItemDto? dto, Guid categoryId)
    {
        dto.ShouldNotBeNull();
        dto.CategoryId.ShouldBe(categoryId);
    }

    private static void ItShouldHaveMonth(BudgetItemDto? dto, int expectedMonth)
    {
        dto.ShouldNotBeNull();
        dto.Month.ShouldBe(expectedMonth);
    }

    private static void ItShouldHaveBudgetFlow(BudgetItemDto? dto, string expectedFlow)
    {
        dto.ShouldNotBeNull();
        dto.BudgetFlow.ShouldBe(expectedFlow);
    }

    private static void ItShouldHavePlannedAmount(BudgetItemDto? dto, decimal expectedAmount, string expectedCurrency)
    {
        dto.ShouldNotBeNull();
        dto.PlannedAmount.ShouldBe(expectedAmount);
        dto.PlannedCurrency.ShouldBe(expectedCurrency);
    }

    private static void ItShouldHavePayerSplitCount(BudgetItemDto? dto, int expectedCount)
    {
        dto.ShouldNotBeNull();
        dto.PayerSplit.Count.ShouldBe(expectedCount);
    }

    private static void ItShouldHaveAttributionSplitCount(BudgetItemDto? dto, int expectedCount)
    {
        dto.ShouldNotBeNull();
        dto.AttributionSplit.Count.ShouldBe(expectedCount);
    }

    private static void ItShouldHaveItemCount(List<BudgetItemDto>? items, int expectedCount)
    {
        items.ShouldNotBeNull();
        items.Count.ShouldBe(expectedCount);
    }

    private static void ItShouldContainItemForMonth(List<BudgetItemDto>? items, int expectedMonth)
    {
        items.ShouldNotBeNull();
        items.ShouldContain(i => i.Month == expectedMonth);
    }
}
