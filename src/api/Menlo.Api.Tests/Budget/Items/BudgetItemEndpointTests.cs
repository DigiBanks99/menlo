using Menlo.Api.Budget.Categories;
using Menlo.Api.Budget.Items;
using Menlo.Lib.Common.ValueObjects;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Menlo.Api.Tests.Budget.Items;

[ClassDataSource<BudgetApiFixture>(Shared = SharedType.PerTestSession)]
[NotInParallel("Budget")]
public sealed class BudgetItemEndpointTests(BudgetApiFixture fixture) : TestFixture
{
    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNameCaseInsensitive = true };

    // =========================================================================
    // CREATE BUDGET ITEM
    // =========================================================================

    [Test]
    public async Task GivenValidRequest_WhenCreateBudgetItem_ThenReturns201WithDto()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current!.Execution.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        Guid categoryId = await CreateLeafCategoryAsync(client, budgetId);
        CreateBudgetItemRequest request = CreateValidItemRequest();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items",
            request,
            TestContext.Current!.Execution.CancellationToken);

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

    [Test]
    public async Task GivenInvalidMonth_WhenCreateBudgetItem_ThenReturns400()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current!.Execution.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        Guid categoryId = await CreateLeafCategoryAsync(client, budgetId);
        CreateBudgetItemRequest request = CreateValidItemRequest(month: 0);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items",
            request,
            TestContext.Current!.Execution.CancellationToken);

        ItShouldHaveReturned400BadRequest(response);
    }

    [Test]
    public async Task GivenNonLeafCategory_WhenCreateBudgetItem_ThenReturns400()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current!.Execution.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        Guid rootCategoryId = await CreateRootWithChildCategoryAsync(client, budgetId);
        CreateBudgetItemRequest request = CreateValidItemRequest();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{rootCategoryId}/items",
            request,
            TestContext.Current!.Execution.CancellationToken);

        ItShouldHaveReturned400BadRequest(response);
    }

    [Test]
    public async Task GivenIncomeCategoryWithExpenseItem_WhenCreate_ThenReturns400()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current!.Execution.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        Guid categoryId = await CreateLeafCategoryAsync(client, budgetId, budgetFlow: "Income");

        // Try to add an Expense item to an Income category
        CreateBudgetItemRequest request = CreateValidItemRequest();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items",
            request,
            TestContext.Current!.Execution.CancellationToken);

        ItShouldHaveReturned400BadRequest(response);
    }

    [Test]
    public async Task GivenExistingItemForMonth_WhenCreateDuplicate_ThenReturns409()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current!.Execution.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        Guid categoryId = await CreateLeafCategoryAsync(client, budgetId);
        CreateBudgetItemRequest request = CreateValidItemRequest(month: 1);

        // First creation should succeed
        HttpResponseMessage firstResponse = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items",
            request,
            TestContext.Current!.Execution.CancellationToken);
        firstResponse.IsSuccessStatusCode.ShouldBeTrue(
            $"First item creation failed: {await firstResponse.Content.ReadAsStringAsync(TestContext.Current!.Execution.CancellationToken)}");

        // Duplicate should fail
        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items",
            request,
            TestContext.Current!.Execution.CancellationToken);

        ItShouldHaveReturned409Conflict(response);
    }

    [Test]
    public async Task GivenInvalidPayerSplit_WhenCreate_ThenReturns400()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current!.Execution.CancellationToken);

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
            TestContext.Current!.Execution.CancellationToken);

        ItShouldHaveReturned400BadRequest(response);
    }

    [Test]
    public async Task GivenInvalidBudgetFlow_WhenCreateBudgetItem_ThenReturns400()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current!.Execution.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        Guid categoryId = await CreateLeafCategoryAsync(client, budgetId);

        CreateBudgetItemRequest request = new(
            Month: 1,
            BudgetFlow: "INVALID_FLOW",
            PlannedAmount: 1500.00m,
            PlannedCurrency: "ZAR",
            PayerSplit: [new PayerAllocationDto(Guid.NewGuid(), 100)],
            AttributionSplit: [new AttributionAllocationDto("Main", 100)]);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items",
            request,
            TestContext.Current!.Execution.CancellationToken);

        ItShouldHaveReturned400BadRequest(response);
    }

    [Test]
    public async Task GivenEmptyPayerSplit_WhenCreateBudgetItem_ThenReturns400()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current!.Execution.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        Guid categoryId = await CreateLeafCategoryAsync(client, budgetId);

        CreateBudgetItemRequest request = new(
            Month: 1,
            BudgetFlow: "Expense",
            PlannedAmount: 1500.00m,
            PlannedCurrency: "ZAR",
            PayerSplit: [],
            AttributionSplit: [new AttributionAllocationDto("Main", 100)]);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items",
            request,
            TestContext.Current!.Execution.CancellationToken);

        ItShouldHaveReturned400BadRequest(response);
    }

    [Test]
    public async Task GivenEmptyAttributionSplit_WhenCreateBudgetItem_ThenReturns400()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current!.Execution.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        Guid categoryId = await CreateLeafCategoryAsync(client, budgetId);

        CreateBudgetItemRequest request = new(
            Month: 1,
            BudgetFlow: "Expense",
            PlannedAmount: 1500.00m,
            PlannedCurrency: "ZAR",
            PayerSplit: [new PayerAllocationDto(Guid.NewGuid(), 100)],
            AttributionSplit: []);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items",
            request,
            TestContext.Current!.Execution.CancellationToken);

        ItShouldHaveReturned400BadRequest(response);
    }

    [Test]
    public async Task GivenInvalidAttributionValue_WhenCreateBudgetItem_ThenReturns400()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current!.Execution.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        Guid categoryId = await CreateLeafCategoryAsync(client, budgetId);

        CreateBudgetItemRequest request = new(
            Month: 1,
            BudgetFlow: "Expense",
            PlannedAmount: 1500.00m,
            PlannedCurrency: "ZAR",
            PayerSplit: [new PayerAllocationDto(Guid.NewGuid(), 100)],
            AttributionSplit: [new AttributionAllocationDto("INVALID_ATTRIBUTION", 100)]);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items",
            request,
            TestContext.Current!.Execution.CancellationToken);

        ItShouldHaveReturned400BadRequest(response);
    }

    // =========================================================================
    // LIST BUDGET ITEMS
    // =========================================================================

    [Test]
    public async Task GivenMultipleItems_WhenListItems_ThenReturnsAllItems()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current!.Execution.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        Guid categoryId = await CreateLeafCategoryAsync(client, budgetId);

        // Create items for months 1, 2, 3
        foreach (int month in new[] { 1, 2, 3 })
        {
            HttpResponseMessage createResponse = await client.PostAsJsonAsync(
                $"/api/budgets/{budgetId}/categories/{categoryId}/items",
                CreateValidItemRequest(month: month),
                TestContext.Current!.Execution.CancellationToken);
            createResponse.IsSuccessStatusCode.ShouldBeTrue(
                $"Item creation for month {month} failed: {await createResponse.Content.ReadAsStringAsync(TestContext.Current!.Execution.CancellationToken)}");
        }

        HttpResponseMessage response = await client.GetAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items",
            TestContext.Current!.Execution.CancellationToken);

        List<BudgetItemDto>? items = await DeserializeBudgetItemList(response);

        ItShouldHaveReturned200Ok(response);
        ItShouldHaveItemCount(items, 3);
        ItShouldContainItemForMonth(items, 1);
        ItShouldContainItemForMonth(items, 2);
        ItShouldContainItemForMonth(items, 3);
    }

    [Test]
    public async Task GivenMultipleItems_WhenListWithMonthFilter_ThenReturnsFilteredItems()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current!.Execution.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        Guid categoryId = await CreateLeafCategoryAsync(client, budgetId);

        // Create items for months 1, 2, 3
        foreach (int month in new[] { 1, 2, 3 })
        {
            HttpResponseMessage createResponse = await client.PostAsJsonAsync(
                $"/api/budgets/{budgetId}/categories/{categoryId}/items",
                CreateValidItemRequest(month: month),
                TestContext.Current!.Execution.CancellationToken);
            createResponse.IsSuccessStatusCode.ShouldBeTrue(
                $"Item creation for month {month} failed: {await createResponse.Content.ReadAsStringAsync(TestContext.Current!.Execution.CancellationToken)}");
        }

        HttpResponseMessage response = await client.GetAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items?month=2",
            TestContext.Current!.Execution.CancellationToken);

        List<BudgetItemDto>? items = await DeserializeBudgetItemList(response);

        ItShouldHaveReturned200Ok(response);
        ItShouldHaveItemCount(items, 1);
        ItShouldContainItemForMonth(items, 2);
    }

    [Test]
    public async Task GivenFeatureFlagOff_WhenListBudgetItems_ThenReturns404()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory();
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current!.Execution.CancellationToken);

        HttpResponseMessage response = await client.GetAsync(
            $"/api/budgets/{Guid.NewGuid()}/categories/{Guid.NewGuid()}/items",
            TestContext.Current!.Execution.CancellationToken);

        ItShouldHaveReturned404NotFound(response);
    }

    [Test]
    public async Task GivenNonExistentBudget_WhenListBudgetItems_ThenReturns404()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current!.Execution.CancellationToken);

        HttpResponseMessage response = await client.GetAsync(
            $"/api/budgets/{Guid.NewGuid()}/categories/{Guid.NewGuid()}/items",
            TestContext.Current!.Execution.CancellationToken);

        ItShouldHaveReturned404NotFound(response);
    }

    // =========================================================================
    // FEATURE FLAG & NOT FOUND
    // =========================================================================

    [Test]
    public async Task GivenFeatureFlagOff_WhenCreateBudgetItem_ThenReturns404()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory();
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current!.Execution.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);

        CreateBudgetItemRequest request = CreateValidItemRequest();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{Guid.NewGuid()}/items",
            request,
            TestContext.Current!.Execution.CancellationToken);

        ItShouldHaveReturned404NotFound(response);
    }

    [Test]
    public async Task GivenNonExistentBudget_WhenCreateBudgetItem_ThenReturns404()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current!.Execution.CancellationToken);

        CreateBudgetItemRequest request = CreateValidItemRequest();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/budgets/{Guid.NewGuid()}/categories/{Guid.NewGuid()}/items",
            request,
            TestContext.Current!.Execution.CancellationToken);

        ItShouldHaveReturned404NotFound(response);
    }

    // =========================================================================
    // AUTHENTICATION AND ANTIFORGERY
    // =========================================================================

    [Test]
    public async Task GivenUnauthenticatedUser_WhenListBudgetItems_ThenReturns401()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true, simulateUnauthenticated: true);
        using HttpClient client = factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        HttpResponseMessage response = await client.GetAsync(
            $"/api/budgets/{Guid.NewGuid()}/categories/{Guid.NewGuid()}/items",
            TestContext.Current!.Execution.CancellationToken);

        ItShouldHaveBeenUnauthorised(response);
    }

    [Test]
    public async Task GivenNoAntiforgeryToken_WhenCreateBudgetItem_ThenReturns400()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/budgets/{Guid.NewGuid()}/categories/{Guid.NewGuid()}/items",
            CreateValidItemRequest(),
            TestContext.Current!.Execution.CancellationToken);

        ItShouldHaveReturned400BadRequest(response);
    }

    [Test]
    public async Task GivenNoAntiforgeryToken_WhenBulkCreateBudgetItems_ThenReturns400()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/budgets/{Guid.NewGuid()}/categories/{Guid.NewGuid()}/items/bulk",
            CreateValidBulkItemRequest(),
            TestContext.Current!.Execution.CancellationToken);

        ItShouldHaveReturned400BadRequest(response);
    }

    [Test]
    public async Task GivenNoAntiforgeryToken_WhenFillForwardBudgetItems_ThenReturns400()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/budgets/{Guid.NewGuid()}/categories/{Guid.NewGuid()}/items/fill-forward",
            CreateValidFillForwardRequest(),
            TestContext.Current!.Execution.CancellationToken);

        ItShouldHaveReturned400BadRequest(response);
    }

    [Test]
    public async Task GivenNoAntiforgeryToken_WhenUpdateBudgetItem_ThenReturns400()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/api/budgets/{Guid.NewGuid()}/categories/{Guid.NewGuid()}/items/{Guid.NewGuid()}",
            new UpdateBudgetItemRequest(PlannedAmount: 1000.00m, PlannedCurrency: "ZAR"),
            TestContext.Current!.Execution.CancellationToken);

        ItShouldHaveReturned400BadRequest(response);
    }

    [Test]
    public async Task GivenNoAntiforgeryToken_WhenRealizeBudgetItem_ThenReturns400()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/api/budgets/{Guid.NewGuid()}/categories/{Guid.NewGuid()}/items/{Guid.NewGuid()}/realize",
            new RealizeBudgetItemRequest(Amount: 1000.00m, Currency: "ZAR"),
            TestContext.Current!.Execution.CancellationToken);

        ItShouldHaveReturned400BadRequest(response);
    }

    [Test]
    public async Task GivenNoAntiforgeryToken_WhenRecordItemSpent_ThenReturns400()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/api/budgets/{Guid.NewGuid()}/categories/{Guid.NewGuid()}/items/{Guid.NewGuid()}/spent",
            new RecordBudgetItemSpentRequest(Amount: 1000.00m, Currency: "ZAR"),
            TestContext.Current!.Execution.CancellationToken);

        ItShouldHaveReturned400BadRequest(response);
    }

    [Test]
    public async Task GivenNoAntiforgeryToken_WhenDeleteBudgetItem_ThenReturns400()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.DeleteAsync(
            $"/api/budgets/{Guid.NewGuid()}/categories/{Guid.NewGuid()}/items/{Guid.NewGuid()}",
            TestContext.Current!.Execution.CancellationToken);

        ItShouldHaveReturned400BadRequest(response);
    }

    // =========================================================================
    // UPDATE BUDGET ITEM
    // =========================================================================

    [Test]
    public async Task GivenExistingItem_WhenUpdatePlannedAmount_ThenReturns200WithUpdatedAmount()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current!.Execution.CancellationToken);

        (Guid budgetId, Guid categoryId, BudgetItemDto createdItem) = await CreateItemForUpdateAsync(client);

        UpdateBudgetItemRequest updateRequest = new(PlannedAmount: 2500.00m, PlannedCurrency: "ZAR");

        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items/{createdItem.Id}",
            updateRequest,
            TestContext.Current!.Execution.CancellationToken);

        BudgetItemDto? dto = await DeserializeBudgetItemDto(response);

        ItShouldHaveReturned200Ok(response);
        ItShouldHavePlannedAmount(dto, 2500.00m, "ZAR");
        ItShouldHavePayerSplitCount(dto, 1);
        ItShouldHaveAttributionSplitCount(dto, 1);
    }

    [Test]
    public async Task GivenExistingItem_WhenUpdatePayerSplit_ThenReturns200WithUpdatedSplit()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current!.Execution.CancellationToken);

        (Guid budgetId, Guid categoryId, BudgetItemDto createdItem) = await CreateItemForUpdateAsync(client);

        Guid payer1 = Guid.NewGuid();
        Guid payer2 = Guid.NewGuid();
        UpdateBudgetItemRequest updateRequest = new(
            PayerSplit: [new PayerAllocationDto(payer1, 60), new PayerAllocationDto(payer2, 40)]);

        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items/{createdItem.Id}",
            updateRequest,
            TestContext.Current!.Execution.CancellationToken);

        BudgetItemDto? dto = await DeserializeBudgetItemDto(response);

        ItShouldHaveReturned200Ok(response);
        ItShouldHavePayerSplitCount(dto, 2);
        ItShouldHavePlannedAmount(dto, createdItem.PlannedAmount, createdItem.PlannedCurrency);
        ItShouldHaveAttributionSplitCount(dto, 1);
    }

    [Test]
    public async Task GivenExistingItem_WhenUpdateAttributionSplit_ThenReturns200WithUpdatedAttribution()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current!.Execution.CancellationToken);

        (Guid budgetId, Guid categoryId, BudgetItemDto createdItem) = await CreateItemForUpdateAsync(client);

        UpdateBudgetItemRequest updateRequest = new(
            AttributionSplit: [new AttributionAllocationDto("Main", 70), new AttributionAllocationDto("Rental", 30)]);

        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items/{createdItem.Id}",
            updateRequest,
            TestContext.Current!.Execution.CancellationToken);

        BudgetItemDto? dto = await DeserializeBudgetItemDto(response);

        ItShouldHaveReturned200Ok(response);
        ItShouldHaveAttributionSplitCount(dto, 2);
        ItShouldHavePlannedAmount(dto, createdItem.PlannedAmount, createdItem.PlannedCurrency);
        ItShouldHavePayerSplitCount(dto, 1);
    }

    [Test]
    public async Task GivenExistingItem_WhenPartialUpdate_ThenLeavesOtherFieldsUnchanged()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current!.Execution.CancellationToken);

        (Guid budgetId, Guid categoryId, BudgetItemDto createdItem) = await CreateItemForUpdateAsync(client);

        UpdateBudgetItemRequest updateRequest = new(PlannedAmount: 3000.00m, PlannedCurrency: "ZAR");

        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items/{createdItem.Id}",
            updateRequest,
            TestContext.Current!.Execution.CancellationToken);

        BudgetItemDto? dto = await DeserializeBudgetItemDto(response);

        ItShouldHaveReturned200Ok(response);
        ItShouldHavePlannedAmount(dto, 3000.00m, "ZAR");
        ItShouldHavePayerSplitMatchingOriginal(dto, createdItem);
        ItShouldHaveAttributionSplitMatchingOriginal(dto, createdItem);
        ItShouldHaveMonth(dto, createdItem.Month);
        ItShouldHaveBudgetFlow(dto, createdItem.BudgetFlow);
    }

    [Test]
    public async Task GivenInvalidPayerSplit_WhenUpdateBudgetItem_ThenReturns400()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current!.Execution.CancellationToken);

        (Guid budgetId, Guid categoryId, BudgetItemDto createdItem) = await CreateItemForUpdateAsync(client);

        UpdateBudgetItemRequest updateRequest = new(
            PayerSplit: [new PayerAllocationDto(Guid.NewGuid(), 60), new PayerAllocationDto(Guid.NewGuid(), 30)]);

        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items/{createdItem.Id}",
            updateRequest,
            TestContext.Current!.Execution.CancellationToken);

        ItShouldHaveReturned400BadRequest(response);
    }

    [Test]
    public async Task GivenInvalidAttributionSplit_WhenUpdateBudgetItem_ThenReturns400()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current!.Execution.CancellationToken);

        (Guid budgetId, Guid categoryId, BudgetItemDto createdItem) = await CreateItemForUpdateAsync(client);

        UpdateBudgetItemRequest updateRequest = new(
            AttributionSplit: [new AttributionAllocationDto("Main", 50), new AttributionAllocationDto("Rental", 30)]);

        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items/{createdItem.Id}",
            updateRequest,
            TestContext.Current!.Execution.CancellationToken);

        ItShouldHaveReturned400BadRequest(response);
    }

    [Test]
    public async Task GivenNonExistentItem_WhenUpdateBudgetItem_ThenReturns404()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current!.Execution.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        Guid categoryId = await CreateLeafCategoryAsync(client, budgetId);

        UpdateBudgetItemRequest updateRequest = new(PlannedAmount: 2000.00m, PlannedCurrency: "ZAR");

        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items/{Guid.NewGuid()}",
            updateRequest,
            TestContext.Current!.Execution.CancellationToken);

        ItShouldHaveReturned404NotFound(response);
    }

    [Test]
    public async Task GivenFeatureFlagOff_WhenUpdateBudgetItem_ThenReturns404()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory();
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current!.Execution.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);

        UpdateBudgetItemRequest updateRequest = new(PlannedAmount: 2000.00m, PlannedCurrency: "ZAR");

        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{Guid.NewGuid()}/items/{Guid.NewGuid()}",
            updateRequest,
            TestContext.Current!.Execution.CancellationToken);

        ItShouldHaveReturned404NotFound(response);
    }

    [Test]
    public async Task GivenExistingItem_WhenUpdateWithEmptyBody_ThenReturns200Unchanged()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current!.Execution.CancellationToken);

        (Guid budgetId, Guid categoryId, BudgetItemDto createdItem) = await CreateItemForUpdateAsync(client);

        UpdateBudgetItemRequest updateRequest = new();

        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items/{createdItem.Id}",
            updateRequest,
            TestContext.Current!.Execution.CancellationToken);

        BudgetItemDto? dto = await DeserializeBudgetItemDto(response);

        ItShouldHaveReturned200Ok(response);
        ItShouldHavePlannedAmount(dto, createdItem.PlannedAmount, createdItem.PlannedCurrency);
        ItShouldHavePayerSplitMatchingOriginal(dto, createdItem);
        ItShouldHaveAttributionSplitMatchingOriginal(dto, createdItem);
    }

    [Test]
    public async Task GivenInvalidAttributionValue_WhenUpdateBudgetItem_ThenReturns400()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current!.Execution.CancellationToken);

        (Guid budgetId, Guid categoryId, BudgetItemDto createdItem) = await CreateItemForUpdateAsync(client);

        UpdateBudgetItemRequest updateRequest = new(
            AttributionSplit: [new AttributionAllocationDto("INVALID_ATTRIBUTION", 100)]);

        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items/{createdItem.Id}",
            updateRequest,
            TestContext.Current!.Execution.CancellationToken);

        ItShouldHaveReturned400BadRequest(response);
    }

    [Test]
    public async Task GivenNonExistentBudget_WhenUpdateBudgetItem_ThenReturns404()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current!.Execution.CancellationToken);

        UpdateBudgetItemRequest updateRequest = new(PlannedAmount: 2000.00m, PlannedCurrency: "ZAR");

        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/api/budgets/{Guid.NewGuid()}/categories/{Guid.NewGuid()}/items/{Guid.NewGuid()}",
            updateRequest,
            TestContext.Current!.Execution.CancellationToken);

        ItShouldHaveReturned404NotFound(response);
    }

    // =========================================================================
    // REALIZE BUDGET ITEM
    // =========================================================================

    [Test]
    public async Task GivenExistingItem_WhenRealize_ThenReturns200WithRealizedAmount()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current!.Execution.CancellationToken);

        (Guid budgetId, Guid categoryId, BudgetItemDto createdItem) = await CreateItemForUpdateAsync(client);

        RealizeBudgetItemRequest realizeRequest = new(Amount: 1450.00m, Currency: "ZAR");

        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items/{createdItem.Id}/realize",
            realizeRequest,
            TestContext.Current!.Execution.CancellationToken);

        BudgetItemDto? dto = await DeserializeBudgetItemDto(response);

        ItShouldHaveReturned200Ok(response);
        ItShouldHaveRealizedAmount(dto, 1450.00m, "ZAR");
        ItShouldHavePlannedAmount(dto, createdItem.PlannedAmount, createdItem.PlannedCurrency);
    }

    [Test]
    public async Task GivenNonExistentItem_WhenRealize_ThenReturns404()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current!.Execution.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        Guid categoryId = await CreateLeafCategoryAsync(client, budgetId);

        RealizeBudgetItemRequest realizeRequest = new(Amount: 1000.00m, Currency: "ZAR");

        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items/{Guid.NewGuid()}/realize",
            realizeRequest,
            TestContext.Current!.Execution.CancellationToken);

        ItShouldHaveReturned404NotFound(response);
    }

    [Test]
    public async Task GivenFeatureFlagOff_WhenRealize_ThenReturns404()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory();
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current!.Execution.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);

        RealizeBudgetItemRequest realizeRequest = new(Amount: 1000.00m, Currency: "ZAR");

        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{Guid.NewGuid()}/items/{Guid.NewGuid()}/realize",
            realizeRequest,
            TestContext.Current!.Execution.CancellationToken);

        ItShouldHaveReturned404NotFound(response);
    }

    [Test]
    public async Task GivenNonExistentBudget_WhenRealize_ThenReturns404()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current!.Execution.CancellationToken);

        RealizeBudgetItemRequest realizeRequest = new(Amount: 1000.00m, Currency: "ZAR");

        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/api/budgets/{Guid.NewGuid()}/categories/{Guid.NewGuid()}/items/{Guid.NewGuid()}/realize",
            realizeRequest,
            TestContext.Current!.Execution.CancellationToken);

        ItShouldHaveReturned404NotFound(response);
    }

    // =========================================================================
    // RECORD SPENT BUDGET ITEM
    // =========================================================================

    [Test]
    public async Task GivenExistingItem_WhenRecordSpent_ThenReturns200WithSpentAmount()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current!.Execution.CancellationToken);

        (Guid budgetId, Guid categoryId, BudgetItemDto createdItem) = await CreateItemForUpdateAsync(client);

        // Record spent directly
        RecordBudgetItemSpentRequest spentRequest = new(Amount: 1500.00m, Currency: "ZAR");

        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items/{createdItem.Id}/spent",
            spentRequest,
            TestContext.Current!.Execution.CancellationToken);

        BudgetItemDto? dto = await DeserializeBudgetItemDto(response);

        ItShouldHaveReturned200Ok(response);
        ItShouldHaveSpentAmount(dto, 1500.00m, "ZAR");
    }

    [Test]
    public async Task GivenExistingItemWithoutRealize_WhenRecordSpent_ThenReturns200()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current!.Execution.CancellationToken);

        (Guid budgetId, Guid categoryId, BudgetItemDto createdItem) = await CreateItemForUpdateAsync(client);

        // Directly record spent without realizing first
        RecordBudgetItemSpentRequest spentRequest = new(Amount: 1400.00m, Currency: "ZAR");

        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items/{createdItem.Id}/spent",
            spentRequest,
            TestContext.Current!.Execution.CancellationToken);

        BudgetItemDto? dto = await DeserializeBudgetItemDto(response);

        ItShouldHaveReturned200Ok(response);
        ItShouldHaveSpentAmount(dto, 1400.00m, "ZAR");
    }

    [Test]
    public async Task GivenNonExistentItem_WhenRecordSpent_ThenReturns404()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current!.Execution.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        Guid categoryId = await CreateLeafCategoryAsync(client, budgetId);

        RecordBudgetItemSpentRequest spentRequest = new(Amount: 1000.00m, Currency: "ZAR");

        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items/{Guid.NewGuid()}/spent",
            spentRequest,
            TestContext.Current!.Execution.CancellationToken);

        ItShouldHaveReturned404NotFound(response);
    }

    [Test]
    public async Task GivenFeatureFlagOff_WhenRecordItemSpent_ThenReturns404()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory();
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current!.Execution.CancellationToken);

        RecordBudgetItemSpentRequest spentRequest = new(Amount: 1000.00m, Currency: "ZAR");

        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/api/budgets/{Guid.NewGuid()}/categories/{Guid.NewGuid()}/items/{Guid.NewGuid()}/spent",
            spentRequest,
            TestContext.Current!.Execution.CancellationToken);

        ItShouldHaveReturned404NotFound(response);
    }

    [Test]
    public async Task GivenNonExistentBudget_WhenRecordItemSpent_ThenReturns404()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current!.Execution.CancellationToken);

        RecordBudgetItemSpentRequest spentRequest = new(Amount: 1000.00m, Currency: "ZAR");

        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/api/budgets/{Guid.NewGuid()}/categories/{Guid.NewGuid()}/items/{Guid.NewGuid()}/spent",
            spentRequest,
            TestContext.Current!.Execution.CancellationToken);

        ItShouldHaveReturned404NotFound(response);
    }

    // =========================================================================
    // FULL LIFECYCLE
    // =========================================================================

    [Test]
    public async Task GivenNewItem_WhenFullLifecycle_ThenAllAmountsPresent()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current!.Execution.CancellationToken);

        (Guid budgetId, Guid categoryId, BudgetItemDto createdItem) = await CreateItemForUpdateAsync(client);

        // Step 1: Realize
        RealizeBudgetItemRequest realizeRequest = new(Amount: 1550.00m, Currency: "ZAR");
        HttpResponseMessage realizeResponse = await client.PutAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items/{createdItem.Id}/realize",
            realizeRequest,
            TestContext.Current!.Execution.CancellationToken);

        BudgetItemDto? realizedDto = await DeserializeBudgetItemDto(realizeResponse);

        ItShouldHaveReturned200Ok(realizeResponse);
        ItShouldHavePlannedAmount(realizedDto, 1500.00m, "ZAR");
        ItShouldHaveRealizedAmount(realizedDto, 1550.00m, "ZAR");

        // Step 2: Record Spent
        RecordBudgetItemSpentRequest spentRequest = new(Amount: 1550.00m, Currency: "ZAR");
        HttpResponseMessage spentResponse = await client.PutAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items/{createdItem.Id}/spent",
            spentRequest,
            TestContext.Current!.Execution.CancellationToken);

        BudgetItemDto? spentDto = await DeserializeBudgetItemDto(spentResponse);

        ItShouldHaveReturned200Ok(spentResponse);
        ItShouldHavePlannedAmount(spentDto, 1500.00m, "ZAR");
        ItShouldHaveSpentAmount(spentDto, 1550.00m, "ZAR");
    }

    // =========================================================================
    // DELETE BUDGET ITEM
    // =========================================================================

    [Test]
    public async Task GivenExistingItem_WhenDelete_ThenReturns204()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current!.Execution.CancellationToken);

        (Guid budgetId, Guid categoryId, BudgetItemDto createdItem) = await CreateItemForUpdateAsync(client);

        HttpResponseMessage response = await client.DeleteAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items/{createdItem.Id}",
            TestContext.Current!.Execution.CancellationToken);

        ItShouldHaveReturned204NoContent(response);
    }

    [Test]
    public async Task GivenNonExistentItem_WhenDelete_ThenReturns404()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current!.Execution.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        Guid categoryId = await CreateLeafCategoryAsync(client, budgetId);

        HttpResponseMessage response = await client.DeleteAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items/{Guid.NewGuid()}",
            TestContext.Current!.Execution.CancellationToken);

        ItShouldHaveReturned404NotFound(response);
    }

    [Test]
    public async Task GivenDeletedItem_WhenListItems_ThenDeletedItemIsExcluded()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current!.Execution.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        Guid categoryId = await CreateLeafCategoryAsync(client, budgetId);

        // Create an item
        HttpResponseMessage createResponse = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items",
            CreateValidItemRequest(month: 1),
            TestContext.Current!.Execution.CancellationToken);
        createResponse.IsSuccessStatusCode.ShouldBeTrue(
            $"Item creation failed: {await createResponse.Content.ReadAsStringAsync(TestContext.Current!.Execution.CancellationToken)}");
        BudgetItemDto? createdItem = await DeserializeBudgetItemDto(createResponse);
        createdItem.ShouldNotBeNull();

        // Delete the item
        HttpResponseMessage deleteResponse = await client.DeleteAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items/{createdItem.Id}",
            TestContext.Current!.Execution.CancellationToken);
        ItShouldHaveReturned204NoContent(deleteResponse);

        // List items — deleted item should not appear
        HttpResponseMessage listResponse = await client.GetAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items",
            TestContext.Current!.Execution.CancellationToken);
        List<BudgetItemDto>? items = await DeserializeBudgetItemList(listResponse);

        ItShouldHaveReturned200Ok(listResponse);
        ItShouldHaveItemCount(items, 0);
    }

    [Test]
    public async Task GivenDeletedItem_WhenCreateSameCategoryAndMonth_ThenSucceeds()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current!.Execution.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        Guid categoryId = await CreateLeafCategoryAsync(client, budgetId);

        // Create item for month 1
        CreateBudgetItemRequest request = CreateValidItemRequest(month: 1);
        HttpResponseMessage createResponse = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items",
            request,
            TestContext.Current!.Execution.CancellationToken);
        createResponse.IsSuccessStatusCode.ShouldBeTrue(
            $"Item creation failed: {await createResponse.Content.ReadAsStringAsync(TestContext.Current!.Execution.CancellationToken)}");
        BudgetItemDto? createdItem = await DeserializeBudgetItemDto(createResponse);
        createdItem.ShouldNotBeNull();

        // Delete the item
        HttpResponseMessage deleteResponse = await client.DeleteAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items/{createdItem.Id}",
            TestContext.Current!.Execution.CancellationToken);
        ItShouldHaveReturned204NoContent(deleteResponse);

        // Re-create item for the same category and month
        HttpResponseMessage recreateResponse = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items",
            request,
            TestContext.Current!.Execution.CancellationToken);

        ItShouldHaveReturned201Created(recreateResponse);
    }

    [Test]
    public async Task GivenFeatureFlagOff_WhenDeleteBudgetItem_ThenReturns404()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory();
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current!.Execution.CancellationToken);

        HttpResponseMessage response = await client.DeleteAsync(
            $"/api/budgets/{Guid.NewGuid()}/categories/{Guid.NewGuid()}/items/{Guid.NewGuid()}",
            TestContext.Current!.Execution.CancellationToken);

        ItShouldHaveReturned404NotFound(response);
    }

    [Test]
    public async Task GivenNonExistentBudget_WhenDeleteBudgetItem_ThenReturns404()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current!.Execution.CancellationToken);

        HttpResponseMessage response = await client.DeleteAsync(
            $"/api/budgets/{Guid.NewGuid()}/categories/{Guid.NewGuid()}/items/{Guid.NewGuid()}",
            TestContext.Current!.Execution.CancellationToken);

        ItShouldHaveReturned404NotFound(response);
    }

    // =========================================================================
    // BULK CREATE BUDGET ITEMS
    // =========================================================================

    [Test]
    public async Task GivenValidRequest_WhenBulkCreateItems_ThenReturns201With12Items()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current!.Execution.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        Guid categoryId = await CreateLeafCategoryAsync(client, budgetId);
        BulkCreateBudgetItemRequest request = CreateValidBulkItemRequest();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items/bulk",
            request,
            TestContext.Current!.Execution.CancellationToken);

        List<BudgetItemDto>? items = await DeserializeBudgetItemList(response);

        ItShouldHaveReturned201Created(response);
        ItShouldHaveItemCount(items, 12);
        for (int month = 1; month <= 12; month++)
        {
            ItShouldContainItemForMonth(items, month);
        }
    }

    [Test]
    public async Task GivenExistingItemForMonth1_WhenBulkCreate_ThenReturns201With11Items()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current!.Execution.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        Guid categoryId = await CreateLeafCategoryAsync(client, budgetId);

        // Create item for month 1 first
        CreateBudgetItemRequest singleRequest = CreateValidItemRequest(month: 1);
        HttpResponseMessage singleResponse = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items",
            singleRequest,
            TestContext.Current!.Execution.CancellationToken);
        singleResponse.IsSuccessStatusCode.ShouldBeTrue(
            $"Single item creation failed: {await singleResponse.Content.ReadAsStringAsync(TestContext.Current!.Execution.CancellationToken)}");

        // Bulk create
        BulkCreateBudgetItemRequest bulkRequest = CreateValidBulkItemRequest();
        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items/bulk",
            bulkRequest,
            TestContext.Current!.Execution.CancellationToken);

        List<BudgetItemDto>? items = await DeserializeBudgetItemList(response);

        ItShouldHaveReturned201Created(response);
        ItShouldHaveItemCount(items, 11);
        ItShouldNotContainItemForMonth(items, 1);
    }

    [Test]
    public async Task GivenInvalidPayerSplit_WhenBulkCreate_ThenReturns400()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current!.Execution.CancellationToken);

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
            TestContext.Current!.Execution.CancellationToken);

        ItShouldHaveReturned400BadRequest(response);
    }

    [Test]
    public async Task GivenNonLeafCategory_WhenBulkCreate_ThenReturns400()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current!.Execution.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        Guid rootCategoryId = await CreateRootWithChildCategoryAsync(client, budgetId);
        BulkCreateBudgetItemRequest request = CreateValidBulkItemRequest();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{rootCategoryId}/items/bulk",
            request,
            TestContext.Current!.Execution.CancellationToken);

        ItShouldHaveReturned400BadRequest(response);
    }

    [Test]
    public async Task GivenFeatureFlagOff_WhenBulkCreateBudgetItems_ThenReturns404()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory();
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current!.Execution.CancellationToken);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/budgets/{Guid.NewGuid()}/categories/{Guid.NewGuid()}/items/bulk",
            CreateValidBulkItemRequest(),
            TestContext.Current!.Execution.CancellationToken);

        ItShouldHaveReturned404NotFound(response);
    }

    [Test]
    public async Task GivenNonExistentBudget_WhenBulkCreate_ThenReturns404()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current!.Execution.CancellationToken);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/budgets/{Guid.NewGuid()}/categories/{Guid.NewGuid()}/items/bulk",
            CreateValidBulkItemRequest(),
            TestContext.Current!.Execution.CancellationToken);

        ItShouldHaveReturned404NotFound(response);
    }

    [Test]
    public async Task GivenInvalidBudgetFlow_WhenBulkCreate_ThenReturns400()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current!.Execution.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        Guid categoryId = await CreateLeafCategoryAsync(client, budgetId);

        BulkCreateBudgetItemRequest request = new(
            BudgetFlow: "INVALID_FLOW",
            Amount: 1500.00m,
            Currency: "ZAR",
            PayerSplit: [new PayerAllocationDto(Guid.NewGuid(), 100)],
            AttributionSplit: [new AttributionAllocationDto("Main", 100)]);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items/bulk",
            request,
            TestContext.Current!.Execution.CancellationToken);

        ItShouldHaveReturned400BadRequest(response);
    }

    [Test]
    public async Task GivenEmptyPayerSplit_WhenBulkCreate_ThenReturns400()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current!.Execution.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        Guid categoryId = await CreateLeafCategoryAsync(client, budgetId);

        BulkCreateBudgetItemRequest request = new(
            BudgetFlow: "Expense",
            Amount: 1500.00m,
            Currency: "ZAR",
            PayerSplit: [],
            AttributionSplit: [new AttributionAllocationDto("Main", 100)]);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items/bulk",
            request,
            TestContext.Current!.Execution.CancellationToken);

        ItShouldHaveReturned400BadRequest(response);
    }

    [Test]
    public async Task GivenEmptyAttributionSplit_WhenBulkCreate_ThenReturns400()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current!.Execution.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        Guid categoryId = await CreateLeafCategoryAsync(client, budgetId);

        BulkCreateBudgetItemRequest request = new(
            BudgetFlow: "Expense",
            Amount: 1500.00m,
            Currency: "ZAR",
            PayerSplit: [new PayerAllocationDto(Guid.NewGuid(), 100)],
            AttributionSplit: []);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items/bulk",
            request,
            TestContext.Current!.Execution.CancellationToken);

        ItShouldHaveReturned400BadRequest(response);
    }

    [Test]
    public async Task GivenInvalidAttributionValue_WhenBulkCreate_ThenReturns400()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current!.Execution.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        Guid categoryId = await CreateLeafCategoryAsync(client, budgetId);

        BulkCreateBudgetItemRequest request = new(
            BudgetFlow: "Expense",
            Amount: 1500.00m,
            Currency: "ZAR",
            PayerSplit: [new PayerAllocationDto(Guid.NewGuid(), 100)],
            AttributionSplit: [new AttributionAllocationDto("INVALID_ATTRIBUTION", 100)]);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items/bulk",
            request,
            TestContext.Current!.Execution.CancellationToken);

        ItShouldHaveReturned400BadRequest(response);
    }

    // =========================================================================
    // FILL FORWARD BUDGET ITEMS
    // =========================================================================

    [Test]
    public async Task GivenValidRequest_WhenFillForwardFromMonth1_ThenReturns200With12Items()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current!.Execution.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        Guid categoryId = await CreateLeafCategoryAsync(client, budgetId);
        FillForwardBudgetItemRequest request = CreateValidFillForwardRequest(fromMonth: 1);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items/fill-forward",
            request,
            TestContext.Current!.Execution.CancellationToken);

        List<BudgetItemDto>? items = await DeserializeBudgetItemList(response);

        ItShouldHaveReturned200Ok(response);
        ItShouldHaveItemCount(items, 12);
    }

    [Test]
    public async Task GivenValidRequest_WhenFillForwardFromMonth6_ThenReturns200With7Items()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current!.Execution.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        Guid categoryId = await CreateLeafCategoryAsync(client, budgetId);
        FillForwardBudgetItemRequest request = CreateValidFillForwardRequest(fromMonth: 6);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items/fill-forward",
            request,
            TestContext.Current!.Execution.CancellationToken);

        List<BudgetItemDto>? items = await DeserializeBudgetItemList(response);

        ItShouldHaveReturned200Ok(response);
        ItShouldHaveItemCount(items, 7);
    }

    [Test]
    public async Task GivenInvalidMonth_WhenFillForward_ThenReturns400()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current!.Execution.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        Guid categoryId = await CreateLeafCategoryAsync(client, budgetId);
        FillForwardBudgetItemRequest request = CreateValidFillForwardRequest(fromMonth: 0);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items/fill-forward",
            request,
            TestContext.Current!.Execution.CancellationToken);

        ItShouldHaveReturned400BadRequest(response);
    }

    [Test]
    public async Task GivenExistingItem_WhenFillForward_ThenUpdatesExistingItemAmount()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current!.Execution.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        Guid categoryId = await CreateLeafCategoryAsync(client, budgetId);

        CreateBudgetItemRequest existingItemRequest = CreateValidItemRequest() with { Month = 3, PlannedAmount = 1500.00m };
        await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items",
            existingItemRequest,
            TestContext.Current!.Execution.CancellationToken);

        FillForwardBudgetItemRequest fillRequest = CreateValidFillForwardRequest(fromMonth: 1, amount: 2000.00m);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items/fill-forward",
            fillRequest,
            TestContext.Current!.Execution.CancellationToken);

        List<BudgetItemDto>? items = await DeserializeBudgetItemList(response);

        ItShouldHaveReturned200Ok(response);
        ItShouldHaveItemCount(items, 12);
        ItShouldHaveAllItemsWithAmount(items, 2000.00m);
    }

    [Test]
    public async Task GivenFeatureFlagOff_WhenFillForward_ThenReturns404()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory();
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current!.Execution.CancellationToken);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/budgets/{Guid.NewGuid()}/categories/{Guid.NewGuid()}/items/fill-forward",
            CreateValidFillForwardRequest(),
            TestContext.Current!.Execution.CancellationToken);

        ItShouldHaveReturned404NotFound(response);
    }

    [Test]
    public async Task GivenNonExistentBudget_WhenFillForward_ThenReturns404()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current!.Execution.CancellationToken);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/budgets/{Guid.NewGuid()}/categories/{Guid.NewGuid()}/items/fill-forward",
            CreateValidFillForwardRequest(),
            TestContext.Current!.Execution.CancellationToken);

        ItShouldHaveReturned404NotFound(response);
    }

    [Test]
    public async Task GivenInvalidBudgetFlow_WhenFillForward_ThenReturns400()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current!.Execution.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        Guid categoryId = await CreateLeafCategoryAsync(client, budgetId);

        FillForwardBudgetItemRequest request = new(
            FromMonth: 1,
            BudgetFlow: "INVALID_FLOW",
            Amount: 1500.00m,
            Currency: "ZAR",
            PayerSplit: [new PayerAllocationDto(Guid.NewGuid(), 100)],
            AttributionSplit: [new AttributionAllocationDto("Main", 100)]);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items/fill-forward",
            request,
            TestContext.Current!.Execution.CancellationToken);

        ItShouldHaveReturned400BadRequest(response);
    }

    [Test]
    public async Task GivenEmptyPayerSplit_WhenFillForward_ThenReturns400()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current!.Execution.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        Guid categoryId = await CreateLeafCategoryAsync(client, budgetId);

        FillForwardBudgetItemRequest request = new(
            FromMonth: 1,
            BudgetFlow: "Expense",
            Amount: 1500.00m,
            Currency: "ZAR",
            PayerSplit: [],
            AttributionSplit: [new AttributionAllocationDto("Main", 100)]);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items/fill-forward",
            request,
            TestContext.Current!.Execution.CancellationToken);

        ItShouldHaveReturned400BadRequest(response);
    }

    [Test]
    public async Task GivenPayerSplitNotSummingTo100_WhenFillForward_ThenReturns400()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current!.Execution.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        Guid categoryId = await CreateLeafCategoryAsync(client, budgetId);

        FillForwardBudgetItemRequest request = new(
            FromMonth: 1,
            BudgetFlow: "Expense",
            Amount: 1500.00m,
            Currency: "ZAR",
            PayerSplit: [new PayerAllocationDto(Guid.NewGuid(), 60), new PayerAllocationDto(Guid.NewGuid(), 30)],
            AttributionSplit: [new AttributionAllocationDto("Main", 100)]);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items/fill-forward",
            request,
            TestContext.Current!.Execution.CancellationToken);

        ItShouldHaveReturned400BadRequest(response);
    }

    [Test]
    public async Task GivenEmptyAttributionSplit_WhenFillForward_ThenReturns400()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current!.Execution.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        Guid categoryId = await CreateLeafCategoryAsync(client, budgetId);

        FillForwardBudgetItemRequest request = new(
            FromMonth: 1,
            BudgetFlow: "Expense",
            Amount: 1500.00m,
            Currency: "ZAR",
            PayerSplit: [new PayerAllocationDto(Guid.NewGuid(), 100)],
            AttributionSplit: []);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items/fill-forward",
            request,
            TestContext.Current!.Execution.CancellationToken);

        ItShouldHaveReturned400BadRequest(response);
    }

    [Test]
    public async Task GivenInvalidAttributionValue_WhenFillForward_ThenReturns400()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current!.Execution.CancellationToken);

        Guid budgetId = await CreateBudgetAsync(client);
        Guid categoryId = await CreateLeafCategoryAsync(client, budgetId);

        FillForwardBudgetItemRequest request = new(
            FromMonth: 1,
            BudgetFlow: "Expense",
            Amount: 1500.00m,
            Currency: "ZAR",
            PayerSplit: [new PayerAllocationDto(Guid.NewGuid(), 100)],
            AttributionSplit: [new AttributionAllocationDto("INVALID_ATTRIBUTION", 100)]);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items/fill-forward",
            request,
            TestContext.Current!.Execution.CancellationToken);

        ItShouldHaveReturned400BadRequest(response);
    }

    // =========================================================================
    // FACTORY HELPERS
    // =========================================================================

    // =========================================================================
    // DATA SETUP HELPERS
    // =========================================================================

    private static async Task<Guid> CreateBudgetAsync(HttpClient client)
    {
        int year = DateTimeOffset.UtcNow.Year;
        HttpResponseMessage response = await client.PostAsync(
            $"/api/budgets/{year}", null, TestContext.Current!.Execution.CancellationToken);
        response.IsSuccessStatusCode.ShouldBeTrue(
            $"Budget creation failed: {await response.Content.ReadAsStringAsync(TestContext.Current!.Execution.CancellationToken)}");

        string content = await response.Content.ReadAsStringAsync(TestContext.Current!.Execution.CancellationToken);
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
            $"/api/budgets/{budgetId}/categories", rootRequest, TestContext.Current!.Execution.CancellationToken);
        rootResponse.IsSuccessStatusCode.ShouldBeTrue(
            $"Root category creation failed: {await rootResponse.Content.ReadAsStringAsync(TestContext.Current!.Execution.CancellationToken)}");
        string rootContent = await rootResponse.Content.ReadAsStringAsync(TestContext.Current!.Execution.CancellationToken);
        Guid rootId = JsonDocument.Parse(rootContent).RootElement.GetProperty("id").GetGuid();

        // Create child (leaf) category
        var childRequest = new CreateCategoryRequest($"Electricity-{suffix}", budgetFlow, ParentId: rootId);
        HttpResponseMessage childResponse = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories", childRequest, TestContext.Current!.Execution.CancellationToken);
        childResponse.IsSuccessStatusCode.ShouldBeTrue(
            $"Child category creation failed: {await childResponse.Content.ReadAsStringAsync(TestContext.Current!.Execution.CancellationToken)}");
        string childContent = await childResponse.Content.ReadAsStringAsync(TestContext.Current!.Execution.CancellationToken);
        return JsonDocument.Parse(childContent).RootElement.GetProperty("id").GetGuid();
    }

    /// <summary>
    /// Creates a root category with a child and returns the ROOT category ID (non-leaf).
    /// </summary>
    private static async Task<Guid> CreateRootWithChildCategoryAsync(HttpClient client, Guid budgetId)
    {
        var rootRequest = new CreateCategoryRequest("Housing", "Expense");
        HttpResponseMessage rootResponse = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories", rootRequest, TestContext.Current!.Execution.CancellationToken);
        rootResponse.IsSuccessStatusCode.ShouldBeTrue(
            $"Root category creation failed: {await rootResponse.Content.ReadAsStringAsync(TestContext.Current!.Execution.CancellationToken)}");
        string rootContent = await rootResponse.Content.ReadAsStringAsync(TestContext.Current!.Execution.CancellationToken);
        Guid rootId = JsonDocument.Parse(rootContent).RootElement.GetProperty("id").GetGuid();

        var childRequest = new CreateCategoryRequest("Rent", "Expense", ParentId: rootId);
        HttpResponseMessage childResponse = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories", childRequest, TestContext.Current!.Execution.CancellationToken);
        childResponse.IsSuccessStatusCode.ShouldBeTrue(
            $"Child category creation failed: {await childResponse.Content.ReadAsStringAsync(TestContext.Current!.Execution.CancellationToken)}");

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
            TestContext.Current!.Execution.CancellationToken);
        createResponse.IsSuccessStatusCode.ShouldBeTrue(
            $"Item creation failed: {await createResponse.Content.ReadAsStringAsync(TestContext.Current!.Execution.CancellationToken)}");

        BudgetItemDto? item = await DeserializeBudgetItemDto(createResponse);
        item.ShouldNotBeNull();

        return (budgetId, categoryId, item);
    }

    // =========================================================================
    // DESERIALIZATION HELPERS
    // =========================================================================

    private static async Task<BudgetItemDto?> DeserializeBudgetItemDto(HttpResponseMessage response)
    {
        string content = await response.Content.ReadAsStringAsync(TestContext.Current!.Execution.CancellationToken);
        return JsonSerializer.Deserialize<BudgetItemDto>(content, JsonOptions);
    }

    private static async Task<List<BudgetItemDto>?> DeserializeBudgetItemList(HttpResponseMessage response)
    {
        string content = await response.Content.ReadAsStringAsync(TestContext.Current!.Execution.CancellationToken);
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
