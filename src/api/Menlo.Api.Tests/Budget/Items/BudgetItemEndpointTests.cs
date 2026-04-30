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

    private static readonly HouseholdId UpdatePlannedAmountHousehold =
        new(Guid.Parse("e1e1e1e1-e1e1-e1e1-e1e1-e1e1e1e1e1e1"));

    private static readonly HouseholdId UpdatePayerSplitHousehold =
        new(Guid.Parse("e2e2e2e2-e2e2-e2e2-e2e2-e2e2e2e2e2e2"));

    private static readonly HouseholdId UpdateAttributionSplitHousehold =
        new(Guid.Parse("e3e3e3e3-e3e3-e3e3-e3e3-e3e3e3e3e3e3"));

    private static readonly HouseholdId UpdatePartialHousehold =
        new(Guid.Parse("e4e4e4e4-e4e4-e4e4-e4e4-e4e4e4e4e4e4"));

    private static readonly HouseholdId UpdateInvalidPayerSplitHousehold =
        new(Guid.Parse("e5e5e5e5-e5e5-e5e5-e5e5-e5e5e5e5e5e5"));

    private static readonly HouseholdId UpdateInvalidAttributionSplitHousehold =
        new(Guid.Parse("e6e6e6e6-e6e6-e6e6-e6e6-e6e6e6e6e6e6"));

    private static readonly HouseholdId UpdateNonExistentItemHousehold =
        new(Guid.Parse("e7e7e7e7-e7e7-e7e7-e7e7-e7e7e7e7e7e7"));

    private static readonly HouseholdId UpdateFeatureOffHousehold =
        new(Guid.Parse("e8e8e8e8-e8e8-e8e8-e8e8-e8e8e8e8e8e8"));

    private static readonly HouseholdId UpdateEmptyBodyHousehold =
        new(Guid.Parse("e9e9e9e9-e9e9-e9e9-e9e9-e9e9e9e9e9e9"));

    private static readonly HouseholdId RealizeItemHousehold =
        new(Guid.Parse("f1f1f1f1-f1f1-f1f1-f1f1-f1f1f1f1f1f1"));

    private static readonly HouseholdId RealizeNonExistentHousehold =
        new(Guid.Parse("f2f2f2f2-f2f2-f2f2-f2f2-f2f2f2f2f2f2"));

    private static readonly HouseholdId RealizeFeatureOffHousehold =
        new(Guid.Parse("f3f3f3f3-f3f3-f3f3-f3f3-f3f3f3f3f3f3"));

    private static readonly HouseholdId RecordSpentHousehold =
        new(Guid.Parse("f4f4f4f4-f4f4-f4f4-f4f4-f4f4f4f4f4f4"));

    private static readonly HouseholdId RecordSpentSkipRealizeHousehold =
        new(Guid.Parse("f5f5f5f5-f5f5-f5f5-f5f5-f5f5f5f5f5f5"));

    private static readonly HouseholdId RecordSpentNonExistentHousehold =
        new(Guid.Parse("f6f6f6f6-f6f6-f6f6-f6f6-f6f6f6f6f6f6"));

    private static readonly HouseholdId FullLifecycleHousehold =
        new(Guid.Parse("f7f7f7f7-f7f7-f7f7-f7f7-f7f7f7f7f7f7"));

    private static readonly HouseholdId DeleteItemHousehold =
        new(Guid.Parse("a1a1a1a1-a1a1-a1a1-a1a1-a1a1a1a1a1a1"));

    private static readonly HouseholdId DeleteNonExistentHousehold =
        new(Guid.Parse("a2a2a2a2-a2a2-a2a2-a2a2-a2a2a2a2a2a2"));

    private static readonly HouseholdId DeleteVerifyListHousehold =
        new(Guid.Parse("a3a3a3a3-a3a3-a3a3-a3a3-a3a3a3a3a3a3"));

    private static readonly HouseholdId DeleteRecreateHousehold =
        new(Guid.Parse("a4a4a4a4-a4a4-a4a4-a4a4-a4a4a4a4a4a4"));

    private static readonly HouseholdId BulkCreateHousehold =
        new(Guid.Parse("b1b1b1b1-b1b1-b1b1-b1b1-b1b1b1b1b1b1"));

    private static readonly HouseholdId BulkCreateWithExistingHousehold =
        new(Guid.Parse("b2b2b2b2-b2b2-b2b2-b2b2-b2b2b2b2b2b2"));

    private static readonly HouseholdId BulkCreateInvalidSplitHousehold =
        new(Guid.Parse("b3b3b3b3-b3b3-b3b3-b3b3-b3b3b3b3b3b3"));

    private static readonly HouseholdId BulkCreateNonLeafHousehold =
        new(Guid.Parse("b4b4b4b4-b4b4-b4b4-b4b4-b4b4b4b4b4b4"));

    private static readonly HouseholdId FillForwardHousehold =
        new(Guid.Parse("c1c1c1c1-c1c1-c1c1-c1c1-c1c1c1c1c1c1"));

    private static readonly HouseholdId FillForwardFromMonth6Household =
        new(Guid.Parse("c2c2c2c2-c2c2-c2c2-c2c2-c2c2c2c2c2c2"));

    private static readonly HouseholdId FillForwardInvalidMonthHousehold =
        new(Guid.Parse("c3c3c3c3-c3c3-c3c3-c3c3-c3c3c3c3c3c3"));

    private static readonly HouseholdId FillForwardUpdatesExistingHousehold =
        new(Guid.Parse("c4c4c4c4-c4c4-c4c4-c4c4-c4c4c4c4c4c4"));

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
    // UPDATE BUDGET ITEM
    // =========================================================================

    [Fact]
    public async Task GivenExistingItem_WhenUpdatePlannedAmount_ThenReturns200WithUpdatedAmount()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(UpdatePlannedAmountHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        (Guid budgetId, Guid categoryId, BudgetItemDto createdItem) = await CreateItemForUpdateAsync(client);

        UpdateBudgetItemRequest updateRequest = new(PlannedAmount: 2500.00m, PlannedCurrency: "ZAR");

        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items/{createdItem.Id}",
            updateRequest,
            TestContext.Current.CancellationToken);

        BudgetItemDto? dto = await DeserializeBudgetItemDto(response);

        ItShouldHaveReturned200Ok(response);
        ItShouldHavePlannedAmount(dto, 2500.00m, "ZAR");
        ItShouldHavePayerSplitCount(dto, 1);
        ItShouldHaveAttributionSplitCount(dto, 1);
    }

    [Fact]
    public async Task GivenExistingItem_WhenUpdatePayerSplit_ThenReturns200WithUpdatedSplit()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(UpdatePayerSplitHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        (Guid budgetId, Guid categoryId, BudgetItemDto createdItem) = await CreateItemForUpdateAsync(client);

        Guid payer1 = Guid.NewGuid();
        Guid payer2 = Guid.NewGuid();
        UpdateBudgetItemRequest updateRequest = new(
            PayerSplit: [new PayerAllocationDto(payer1, 60), new PayerAllocationDto(payer2, 40)]);

        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items/{createdItem.Id}",
            updateRequest,
            TestContext.Current.CancellationToken);

        BudgetItemDto? dto = await DeserializeBudgetItemDto(response);

        ItShouldHaveReturned200Ok(response);
        ItShouldHavePayerSplitCount(dto, 2);
        ItShouldHavePlannedAmount(dto, createdItem.PlannedAmount, createdItem.PlannedCurrency);
        ItShouldHaveAttributionSplitCount(dto, 1);
    }

    [Fact]
    public async Task GivenExistingItem_WhenUpdateAttributionSplit_ThenReturns200WithUpdatedAttribution()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(UpdateAttributionSplitHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        (Guid budgetId, Guid categoryId, BudgetItemDto createdItem) = await CreateItemForUpdateAsync(client);

        UpdateBudgetItemRequest updateRequest = new(
            AttributionSplit: [new AttributionAllocationDto("Main", 70), new AttributionAllocationDto("Rental", 30)]);

        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items/{createdItem.Id}",
            updateRequest,
            TestContext.Current.CancellationToken);

        BudgetItemDto? dto = await DeserializeBudgetItemDto(response);

        ItShouldHaveReturned200Ok(response);
        ItShouldHaveAttributionSplitCount(dto, 2);
        ItShouldHavePlannedAmount(dto, createdItem.PlannedAmount, createdItem.PlannedCurrency);
        ItShouldHavePayerSplitCount(dto, 1);
    }

    [Fact]
    public async Task GivenExistingItem_WhenPartialUpdate_ThenLeavesOtherFieldsUnchanged()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(UpdatePartialHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        (Guid budgetId, Guid categoryId, BudgetItemDto createdItem) = await CreateItemForUpdateAsync(client);

        UpdateBudgetItemRequest updateRequest = new(PlannedAmount: 3000.00m, PlannedCurrency: "ZAR");

        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items/{createdItem.Id}",
            updateRequest,
            TestContext.Current.CancellationToken);

        BudgetItemDto? dto = await DeserializeBudgetItemDto(response);

        ItShouldHaveReturned200Ok(response);
        ItShouldHavePlannedAmount(dto, 3000.00m, "ZAR");
        ItShouldHavePayerSplitMatchingOriginal(dto, createdItem);
        ItShouldHaveAttributionSplitMatchingOriginal(dto, createdItem);
        ItShouldHaveMonth(dto, createdItem.Month);
        ItShouldHaveBudgetFlow(dto, createdItem.BudgetFlow);
    }

    [Fact]
    public async Task GivenInvalidPayerSplit_WhenUpdateBudgetItem_ThenReturns400()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(UpdateInvalidPayerSplitHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        (Guid budgetId, Guid categoryId, BudgetItemDto createdItem) = await CreateItemForUpdateAsync(client);

        UpdateBudgetItemRequest updateRequest = new(
            PayerSplit: [new PayerAllocationDto(Guid.NewGuid(), 60), new PayerAllocationDto(Guid.NewGuid(), 30)]);

        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items/{createdItem.Id}",
            updateRequest,
            TestContext.Current.CancellationToken);

        ItShouldHaveReturned400BadRequest(response);
    }

    [Fact]
    public async Task GivenInvalidAttributionSplit_WhenUpdateBudgetItem_ThenReturns400()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(UpdateInvalidAttributionSplitHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        (Guid budgetId, Guid categoryId, BudgetItemDto createdItem) = await CreateItemForUpdateAsync(client);

        UpdateBudgetItemRequest updateRequest = new(
            AttributionSplit: [new AttributionAllocationDto("Main", 50), new AttributionAllocationDto("Rental", 30)]);

        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items/{createdItem.Id}",
            updateRequest,
            TestContext.Current.CancellationToken);

        ItShouldHaveReturned400BadRequest(response);
    }

    [Fact]
    public async Task GivenNonExistentItem_WhenUpdateBudgetItem_ThenReturns404()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(UpdateNonExistentItemHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        Guid categoryId = await CreateLeafCategoryAsync(client, budgetId);

        UpdateBudgetItemRequest updateRequest = new(PlannedAmount: 2000.00m, PlannedCurrency: "ZAR");

        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items/{Guid.NewGuid()}",
            updateRequest,
            TestContext.Current.CancellationToken);

        ItShouldHaveReturned404NotFound(response);
    }

    [Fact]
    public async Task GivenFeatureFlagOff_WhenUpdateBudgetItem_ThenReturns404()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactoryWithoutBudgetItemsFeature(UpdateFeatureOffHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);

        UpdateBudgetItemRequest updateRequest = new(PlannedAmount: 2000.00m, PlannedCurrency: "ZAR");

        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{Guid.NewGuid()}/items/{Guid.NewGuid()}",
            updateRequest,
            TestContext.Current.CancellationToken);

        ItShouldHaveReturned404NotFound(response);
    }

    [Fact]
    public async Task GivenExistingItem_WhenUpdateWithEmptyBody_ThenReturns200Unchanged()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(UpdateEmptyBodyHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        (Guid budgetId, Guid categoryId, BudgetItemDto createdItem) = await CreateItemForUpdateAsync(client);

        UpdateBudgetItemRequest updateRequest = new();

        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items/{createdItem.Id}",
            updateRequest,
            TestContext.Current.CancellationToken);

        BudgetItemDto? dto = await DeserializeBudgetItemDto(response);

        ItShouldHaveReturned200Ok(response);
        ItShouldHavePlannedAmount(dto, createdItem.PlannedAmount, createdItem.PlannedCurrency);
        ItShouldHavePayerSplitMatchingOriginal(dto, createdItem);
        ItShouldHaveAttributionSplitMatchingOriginal(dto, createdItem);
    }

    // =========================================================================
    // REALIZE BUDGET ITEM
    // =========================================================================

    [Fact]
    public async Task GivenExistingItem_WhenRealize_ThenReturns200WithRealizedAmount()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(RealizeItemHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        (Guid budgetId, Guid categoryId, BudgetItemDto createdItem) = await CreateItemForUpdateAsync(client);

        RealizeBudgetItemRequest realizeRequest = new(Amount: 1450.00m, Currency: "ZAR");

        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items/{createdItem.Id}/realize",
            realizeRequest,
            TestContext.Current.CancellationToken);

        BudgetItemDto? dto = await DeserializeBudgetItemDto(response);

        ItShouldHaveReturned200Ok(response);
        ItShouldHaveRealizedAmount(dto, 1450.00m, "ZAR");
        ItShouldHavePlannedAmount(dto, createdItem.PlannedAmount, createdItem.PlannedCurrency);
    }

    [Fact]
    public async Task GivenNonExistentItem_WhenRealize_ThenReturns404()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(RealizeNonExistentHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        Guid categoryId = await CreateLeafCategoryAsync(client, budgetId);

        RealizeBudgetItemRequest realizeRequest = new(Amount: 1000.00m, Currency: "ZAR");

        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items/{Guid.NewGuid()}/realize",
            realizeRequest,
            TestContext.Current.CancellationToken);

        ItShouldHaveReturned404NotFound(response);
    }

    [Fact]
    public async Task GivenFeatureFlagOff_WhenRealize_ThenReturns404()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactoryWithoutBudgetItemsFeature(RealizeFeatureOffHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);

        RealizeBudgetItemRequest realizeRequest = new(Amount: 1000.00m, Currency: "ZAR");

        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{Guid.NewGuid()}/items/{Guid.NewGuid()}/realize",
            realizeRequest,
            TestContext.Current.CancellationToken);

        ItShouldHaveReturned404NotFound(response);
    }

    // =========================================================================
    // RECORD SPENT BUDGET ITEM
    // =========================================================================

    [Fact]
    public async Task GivenExistingItem_WhenRecordSpent_ThenReturns200WithSpentAmount()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(RecordSpentHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        (Guid budgetId, Guid categoryId, BudgetItemDto createdItem) = await CreateItemForUpdateAsync(client);

        // Record spent directly
        RecordBudgetItemSpentRequest spentRequest = new(Amount: 1500.00m, Currency: "ZAR");

        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items/{createdItem.Id}/spent",
            spentRequest,
            TestContext.Current.CancellationToken);

        BudgetItemDto? dto = await DeserializeBudgetItemDto(response);

        ItShouldHaveReturned200Ok(response);
        ItShouldHaveSpentAmount(dto, 1500.00m, "ZAR");
    }

    [Fact]
    public async Task GivenExistingItemWithoutRealize_WhenRecordSpent_ThenReturns200()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(RecordSpentSkipRealizeHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        (Guid budgetId, Guid categoryId, BudgetItemDto createdItem) = await CreateItemForUpdateAsync(client);

        // Directly record spent without realizing first
        RecordBudgetItemSpentRequest spentRequest = new(Amount: 1400.00m, Currency: "ZAR");

        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items/{createdItem.Id}/spent",
            spentRequest,
            TestContext.Current.CancellationToken);

        BudgetItemDto? dto = await DeserializeBudgetItemDto(response);

        ItShouldHaveReturned200Ok(response);
        ItShouldHaveSpentAmount(dto, 1400.00m, "ZAR");
    }

    [Fact]
    public async Task GivenNonExistentItem_WhenRecordSpent_ThenReturns404()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(RecordSpentNonExistentHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        Guid categoryId = await CreateLeafCategoryAsync(client, budgetId);

        RecordBudgetItemSpentRequest spentRequest = new(Amount: 1000.00m, Currency: "ZAR");

        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items/{Guid.NewGuid()}/spent",
            spentRequest,
            TestContext.Current.CancellationToken);

        ItShouldHaveReturned404NotFound(response);
    }

    // =========================================================================
    // FULL LIFECYCLE
    // =========================================================================

    [Fact]
    public async Task GivenNewItem_WhenFullLifecycle_ThenAllAmountsPresent()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(FullLifecycleHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        (Guid budgetId, Guid categoryId, BudgetItemDto createdItem) = await CreateItemForUpdateAsync(client);

        // Step 1: Realize
        RealizeBudgetItemRequest realizeRequest = new(Amount: 1550.00m, Currency: "ZAR");
        HttpResponseMessage realizeResponse = await client.PutAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items/{createdItem.Id}/realize",
            realizeRequest,
            TestContext.Current.CancellationToken);

        BudgetItemDto? realizedDto = await DeserializeBudgetItemDto(realizeResponse);

        ItShouldHaveReturned200Ok(realizeResponse);
        ItShouldHavePlannedAmount(realizedDto, 1500.00m, "ZAR");
        ItShouldHaveRealizedAmount(realizedDto, 1550.00m, "ZAR");

        // Step 2: Record Spent
        RecordBudgetItemSpentRequest spentRequest = new(Amount: 1550.00m, Currency: "ZAR");
        HttpResponseMessage spentResponse = await client.PutAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items/{createdItem.Id}/spent",
            spentRequest,
            TestContext.Current.CancellationToken);

        BudgetItemDto? spentDto = await DeserializeBudgetItemDto(spentResponse);

        ItShouldHaveReturned200Ok(spentResponse);
        ItShouldHavePlannedAmount(spentDto, 1500.00m, "ZAR");
        ItShouldHaveSpentAmount(spentDto, 1550.00m, "ZAR");
    }

    // =========================================================================
    // DELETE BUDGET ITEM
    // =========================================================================

    [Fact]
    public async Task GivenExistingItem_WhenDelete_ThenReturns204()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(DeleteItemHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        (Guid budgetId, Guid categoryId, BudgetItemDto createdItem) = await CreateItemForUpdateAsync(client);

        HttpResponseMessage response = await client.DeleteAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items/{createdItem.Id}",
            TestContext.Current.CancellationToken);

        ItShouldHaveReturned204NoContent(response);
    }

    [Fact]
    public async Task GivenNonExistentItem_WhenDelete_ThenReturns404()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(DeleteNonExistentHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        Guid categoryId = await CreateLeafCategoryAsync(client, budgetId);

        HttpResponseMessage response = await client.DeleteAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items/{Guid.NewGuid()}",
            TestContext.Current.CancellationToken);

        ItShouldHaveReturned404NotFound(response);
    }

    [Fact]
    public async Task GivenDeletedItem_WhenListItems_ThenDeletedItemIsExcluded()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(DeleteVerifyListHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        Guid categoryId = await CreateLeafCategoryAsync(client, budgetId);

        // Create an item
        HttpResponseMessage createResponse = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items",
            CreateValidItemRequest(month: 1),
            TestContext.Current.CancellationToken);
        createResponse.IsSuccessStatusCode.ShouldBeTrue(
            $"Item creation failed: {await createResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)}");
        BudgetItemDto? createdItem = await DeserializeBudgetItemDto(createResponse);
        createdItem.ShouldNotBeNull();

        // Delete the item
        HttpResponseMessage deleteResponse = await client.DeleteAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items/{createdItem.Id}",
            TestContext.Current.CancellationToken);
        ItShouldHaveReturned204NoContent(deleteResponse);

        // List items — deleted item should not appear
        HttpResponseMessage listResponse = await client.GetAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items",
            TestContext.Current.CancellationToken);
        List<BudgetItemDto>? items = await DeserializeBudgetItemList(listResponse);

        ItShouldHaveReturned200Ok(listResponse);
        ItShouldHaveItemCount(items, 0);
    }

    [Fact]
    public async Task GivenDeletedItem_WhenCreateSameCategoryAndMonth_ThenSucceeds()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(DeleteRecreateHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        Guid categoryId = await CreateLeafCategoryAsync(client, budgetId);

        // Create item for month 1
        CreateBudgetItemRequest request = CreateValidItemRequest(month: 1);
        HttpResponseMessage createResponse = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items",
            request,
            TestContext.Current.CancellationToken);
        createResponse.IsSuccessStatusCode.ShouldBeTrue(
            $"Item creation failed: {await createResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)}");
        BudgetItemDto? createdItem = await DeserializeBudgetItemDto(createResponse);
        createdItem.ShouldNotBeNull();

        // Delete the item
        HttpResponseMessage deleteResponse = await client.DeleteAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items/{createdItem.Id}",
            TestContext.Current.CancellationToken);
        ItShouldHaveReturned204NoContent(deleteResponse);

        // Re-create item for the same category and month
        HttpResponseMessage recreateResponse = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items",
            request,
            TestContext.Current.CancellationToken);

        ItShouldHaveReturned201Created(recreateResponse);
    }

    // =========================================================================
    // BULK CREATE BUDGET ITEMS
    // =========================================================================

    [Fact]
    public async Task GivenValidRequest_WhenBulkCreateItems_ThenReturns201With12Items()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(BulkCreateHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        Guid categoryId = await CreateLeafCategoryAsync(client, budgetId);
        BulkCreateBudgetItemRequest request = CreateValidBulkItemRequest();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items/bulk",
            request,
            TestContext.Current.CancellationToken);

        List<BudgetItemDto>? items = await DeserializeBudgetItemList(response);

        ItShouldHaveReturned201Created(response);
        ItShouldHaveItemCount(items, 12);
        for (int month = 1; month <= 12; month++)
        {
            ItShouldContainItemForMonth(items, month);
        }
    }

    [Fact]
    public async Task GivenExistingItemForMonth1_WhenBulkCreate_ThenReturns201With11Items()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(BulkCreateWithExistingHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        Guid categoryId = await CreateLeafCategoryAsync(client, budgetId);

        // Create item for month 1 first
        CreateBudgetItemRequest singleRequest = CreateValidItemRequest(month: 1);
        HttpResponseMessage singleResponse = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items",
            singleRequest,
            TestContext.Current.CancellationToken);
        singleResponse.IsSuccessStatusCode.ShouldBeTrue(
            $"Single item creation failed: {await singleResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)}");

        // Bulk create
        BulkCreateBudgetItemRequest bulkRequest = CreateValidBulkItemRequest();
        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items/bulk",
            bulkRequest,
            TestContext.Current.CancellationToken);

        List<BudgetItemDto>? items = await DeserializeBudgetItemList(response);

        ItShouldHaveReturned201Created(response);
        ItShouldHaveItemCount(items, 11);
        ItShouldNotContainItemForMonth(items, 1);
    }

    [Fact]
    public async Task GivenInvalidPayerSplit_WhenBulkCreate_ThenReturns400()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(BulkCreateInvalidSplitHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        Guid categoryId = await CreateLeafCategoryAsync(client, budgetId);

        // Payer split that doesn't sum to 100%
        BulkCreateBudgetItemRequest request = new(
            BudgetFlow: "Expense",
            Amount: 1500.00m,
            Currency: "ZAR",
            PayerSplit: [new PayerAllocationDto(Guid.NewGuid(), 60), new PayerAllocationDto(Guid.NewGuid(), 30)],
            AttributionSplit: [new AttributionAllocationDto("Main", 100)]);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items/bulk",
            request,
            TestContext.Current.CancellationToken);

        ItShouldHaveReturned400BadRequest(response);
    }

    [Fact]
    public async Task GivenNonLeafCategory_WhenBulkCreate_ThenReturns400()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(BulkCreateNonLeafHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        Guid rootCategoryId = await CreateRootWithChildCategoryAsync(client, budgetId);
        BulkCreateBudgetItemRequest request = CreateValidBulkItemRequest();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{rootCategoryId}/items/bulk",
            request,
            TestContext.Current.CancellationToken);

        ItShouldHaveReturned400BadRequest(response);
    }

    // =========================================================================
    // FILL FORWARD BUDGET ITEMS
    // =========================================================================

    [Fact]
    public async Task GivenValidRequest_WhenFillForwardFromMonth1_ThenReturns200With12Items()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(FillForwardHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        Guid categoryId = await CreateLeafCategoryAsync(client, budgetId);
        FillForwardBudgetItemRequest request = CreateValidFillForwardRequest(fromMonth: 1);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items/fill-forward",
            request,
            TestContext.Current.CancellationToken);

        List<BudgetItemDto>? items = await DeserializeBudgetItemList(response);

        ItShouldHaveReturned200Ok(response);
        ItShouldHaveItemCount(items, 12);
    }

    [Fact]
    public async Task GivenValidRequest_WhenFillForwardFromMonth6_ThenReturns200With7Items()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(FillForwardFromMonth6Household);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        Guid categoryId = await CreateLeafCategoryAsync(client, budgetId);
        FillForwardBudgetItemRequest request = CreateValidFillForwardRequest(fromMonth: 6);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items/fill-forward",
            request,
            TestContext.Current.CancellationToken);

        List<BudgetItemDto>? items = await DeserializeBudgetItemList(response);

        ItShouldHaveReturned200Ok(response);
        ItShouldHaveItemCount(items, 7);
    }

    [Fact]
    public async Task GivenInvalidMonth_WhenFillForward_ThenReturns400()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(FillForwardInvalidMonthHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        Guid categoryId = await CreateLeafCategoryAsync(client, budgetId);
        FillForwardBudgetItemRequest request = CreateValidFillForwardRequest(fromMonth: 0);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items/fill-forward",
            request,
            TestContext.Current.CancellationToken);

        ItShouldHaveReturned400BadRequest(response);
    }

    [Fact]
    public async Task GivenExistingItem_WhenFillForward_ThenUpdatesExistingItemAmount()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(FillForwardUpdatesExistingHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        Guid categoryId = await CreateLeafCategoryAsync(client, budgetId);

        CreateBudgetItemRequest existingItemRequest = CreateValidItemRequest() with { Month = 3, PlannedAmount = 1500.00m };
        await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items",
            existingItemRequest,
            TestContext.Current.CancellationToken);

        FillForwardBudgetItemRequest fillRequest = CreateValidFillForwardRequest(fromMonth: 1, amount: 2000.00m);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items/fill-forward",
            fillRequest,
            TestContext.Current.CancellationToken);

        List<BudgetItemDto>? items = await DeserializeBudgetItemList(response);

        ItShouldHaveReturned200Ok(response);
        ItShouldHaveItemCount(items, 12);
        ItShouldHaveAllItemsWithAmount(items, 2000.00m);
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
        // Create root category with unique name to avoid collisions in parallel tests
        string suffix = Guid.NewGuid().ToString("N")[..8];
        var rootRequest = new CreateCategoryRequest($"Expenses-{suffix}", budgetFlow);
        HttpResponseMessage rootResponse = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories", rootRequest, TestContext.Current.CancellationToken);
        rootResponse.IsSuccessStatusCode.ShouldBeTrue(
            $"Root category creation failed: {await rootResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)}");
        string rootContent = await rootResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Guid rootId = JsonDocument.Parse(rootContent).RootElement.GetProperty("id").GetGuid();

        // Create child (leaf) category
        var childRequest = new CreateCategoryRequest($"Electricity-{suffix}", budgetFlow, ParentId: rootId);
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

    private static BulkCreateBudgetItemRequest CreateValidBulkItemRequest() =>
        new(
            BudgetFlow: "Expense",
            Amount: 1500.00m,
            Currency: "ZAR",
            PayerSplit: [new PayerAllocationDto(Guid.NewGuid(), 100)],
            AttributionSplit: [new AttributionAllocationDto("Main", 100)]);

    private static FillForwardBudgetItemRequest CreateValidFillForwardRequest(int fromMonth = 1, decimal amount = 1500.00m) =>
        new(
            FromMonth: fromMonth,
            BudgetFlow: "Expense",
            Amount: amount,
            Currency: "ZAR",
            PayerSplit: [new PayerAllocationDto(Guid.NewGuid(), 100)],
            AttributionSplit: [new AttributionAllocationDto("Main", 100)]);

    private static async Task<(Guid BudgetId, Guid CategoryId, BudgetItemDto Item)> CreateItemForUpdateAsync(HttpClient client)
    {
        Guid budgetId = await CreateBudgetAsync(client);
        Guid categoryId = await CreateLeafCategoryAsync(client, budgetId);
        CreateBudgetItemRequest request = CreateValidItemRequest();

        HttpResponseMessage createResponse = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items",
            request,
            TestContext.Current.CancellationToken);
        createResponse.IsSuccessStatusCode.ShouldBeTrue(
            $"Item creation failed: {await createResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)}");

        BudgetItemDto? item = await DeserializeBudgetItemDto(createResponse);
        item.ShouldNotBeNull();

        return (budgetId, categoryId, item);
    }

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

    private static void ItShouldHaveReturned204NoContent(HttpResponseMessage response) =>
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

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

    private static void ItShouldNotContainItemForMonth(List<BudgetItemDto>? items, int month)
    {
        items.ShouldNotBeNull();
        items.ShouldNotContain(i => i.Month == month);
    }

    private static void ItShouldHavePayerSplitMatchingOriginal(BudgetItemDto? dto, BudgetItemDto original)
    {
        dto.ShouldNotBeNull();
        dto.PayerSplit.Count.ShouldBe(original.PayerSplit.Count);
    }

    private static void ItShouldHaveAttributionSplitMatchingOriginal(BudgetItemDto? dto, BudgetItemDto original)
    {
        dto.ShouldNotBeNull();
        dto.AttributionSplit.Count.ShouldBe(original.AttributionSplit.Count);
    }

    private static void ItShouldHaveRealizedAmount(BudgetItemDto? dto, decimal expectedAmount, string expectedCurrency)
    {
        dto.ShouldNotBeNull();
        dto.RealizedAmount.ShouldBe(expectedAmount);
        dto.RealizedCurrency.ShouldBe(expectedCurrency);
    }

    private static void ItShouldHaveSpentAmount(BudgetItemDto? dto, decimal expectedAmount, string expectedCurrency)
    {
        dto.ShouldNotBeNull();
        dto.SpentAmount.ShouldBe(expectedAmount);
        dto.SpentCurrency.ShouldBe(expectedCurrency);
    }

    private static void ItShouldHaveAllItemsWithAmount(List<BudgetItemDto>? items, decimal expectedAmount)
    {
        items.ShouldNotBeNull();
        items.ShouldAllBe(i => i.PlannedAmount == expectedAmount);
    }
}
