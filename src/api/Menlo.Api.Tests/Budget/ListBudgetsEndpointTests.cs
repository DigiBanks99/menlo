using Menlo.Api.Budget;
using Menlo.Api.Budget.Categories;
using Menlo.Api.Budget.Items;
using Menlo.Lib.Common.ValueObjects;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Menlo.Api.Tests.Budget;

[Collection("Budget")]
public sealed class ListBudgetsEndpointTests(BudgetApiFixture fixture) : TestFixture
{
    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNameCaseInsensitive = true };

    // Unique household IDs per scenario — prefixes 1e–4e and 6e avoid all existing test conflicts.
    private static readonly HouseholdId MultipleYearsHousehold =
        new(Guid.Parse("1e1e1e1e-1e1e-1e1e-1e1e-1e1e1e1e1e1e"));

    private static readonly HouseholdId EmptyHousehold =
        new(Guid.Parse("2e2e2e2e-2e2e-2e2e-2e2e-2e2e2e2e2e2e"));

    private static readonly HouseholdId ForeignHouseholdOwner =
        new(Guid.Parse("3e3e3e3e-3e3e-3e3e-3e3e-3e3e3e3e3e3e"));

    private static readonly HouseholdId ForeignHouseholdViewer =
        new(Guid.Parse("4e4e4e4e-4e4e-4e4e-4e4e-4e4e4e4e4e4e"));

    private static readonly HouseholdId ItemsFeatureOffHousehold =
        new(Guid.Parse("6e6e6e6e-6e6e-6e6e-6e6e-6e6e6e6e6e6e"));

    // -------------------------------------------------------------------------
    // DB-backed scenarios
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GivenBudgetsForTwoYears_WhenListBudgets_ThenReturnsNewestFirstWithCurrentMonthExpenses()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(MultipleYearsHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: TestContext.Current.CancellationToken);
        int currentYear = DateTimeOffset.UtcNow.Year;
        int currentMonth = DateTime.UtcNow.Month;
        int otherMonth = currentMonth == 12 ? 1 : currentMonth + 1;

        // Next year first, so the current year has no earlier budget to clone from
        Guid nextYearId = await CreateBudgetAsync(client, currentYear + 1);
        Guid currentYearId = await CreateBudgetAsync(client, currentYear);

        (_, Guid incomeLeafId) = await CreateRootAndLeafAsync(client, currentYearId, "Salary", "Income");
        await CreateItemAsync(client, currentYearId, incomeLeafId, "Income", currentMonth, 9000m);
        (_, Guid expenseLeafId) = await CreateRootAndLeafAsync(client, currentYearId, "Housing", "Expense");
        Guid rentId = await CreateItemAsync(client, currentYearId, expenseLeafId, "Expense", currentMonth, 1500m);
        await CreateItemAsync(client, currentYearId, expenseLeafId, "Expense", currentMonth, 500m);
        await CreateItemAsync(client, currentYearId, expenseLeafId, "Expense", otherMonth, 700m);
        await RecordSpentAsync(client, currentYearId, expenseLeafId, rentId, 1400m);

        HttpResponseMessage response = await client.GetAsync("/api/budgets", TestContext.Current.CancellationToken);
        List<BudgetListItem>? budgets = await DeserializeAsync(response);

        ItShouldHaveReturned200Ok(response);
        ItShouldListTheBudgetsNewestFirst(budgets, nextYearId, currentYearId);
        ItShouldHaveTheCurrentMonthExpenses(budgets![1]);
        ItShouldHaveNoExpensesForNextYear(budgets[0]);

        void ItShouldHaveTheCurrentMonthExpenses(BudgetListItem budget)
        {
            budget.Year.ShouldBe(currentYear);
            budget.Status.ShouldBe("Draft");
            budget.PlannedExpenses.ShouldBe(new MoneyDto(2000m, "ZAR"));
            budget.SpentExpenses.ShouldBe(new MoneyDto(1400m, "ZAR"));
            budget.CategoryCount.ShouldBe(4);
        }
    }

    [Fact]
    public async Task GivenNoBudgets_WhenListBudgets_ThenReturnsEmptyList()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(EmptyHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: TestContext.Current.CancellationToken);

        HttpResponseMessage response = await client.GetAsync("/api/budgets", TestContext.Current.CancellationToken);
        List<BudgetListItem>? budgets = await DeserializeAsync(response);

        ItShouldHaveReturned200Ok(response);
        budgets.ShouldNotBeNull();
        budgets.ShouldBeEmpty();
    }

    [Fact]
    public async Task GivenAnotherHouseholdsBudget_WhenListBudgets_ThenItIsNotReturned()
    {
        await using (BudgetTestWebApplicationFactory ownerFactory = CreateIsolatedFactory(ForeignHouseholdOwner))
        {
            using HttpClient ownerClient = await ownerFactory.CreateAntiforgeryClientAsync(cancellationToken: TestContext.Current.CancellationToken);
            await CreateBudgetAsync(ownerClient, DateTimeOffset.UtcNow.Year);
        }

        await using BudgetTestWebApplicationFactory viewerFactory = CreateIsolatedFactory(ForeignHouseholdViewer);
        using HttpClient viewerClient = await viewerFactory.CreateAntiforgeryClientAsync(cancellationToken: TestContext.Current.CancellationToken);

        HttpResponseMessage response = await viewerClient.GetAsync("/api/budgets", TestContext.Current.CancellationToken);
        List<BudgetListItem>? budgets = await DeserializeAsync(response);

        ItShouldHaveReturned200Ok(response);
        budgets.ShouldNotBeNull();
        budgets.ShouldBeEmpty();
    }

    [Fact]
    public async Task GivenBudgetItemsFeatureOff_WhenListBudgets_ThenItemFiguresAreNotExposed()
    {
        await using (BudgetTestWebApplicationFactory seedFactory = CreateIsolatedFactory(ItemsFeatureOffHousehold))
        {
            using HttpClient seedClient = await seedFactory.CreateAntiforgeryClientAsync(cancellationToken: TestContext.Current.CancellationToken);
            Guid budgetId = await CreateBudgetAsync(seedClient, DateTimeOffset.UtcNow.Year);
            (_, Guid leafId) = await CreateRootAndLeafAsync(seedClient, budgetId, "Housing", "Expense");
            Guid itemId = await CreateItemAsync(seedClient, budgetId, leafId, "Expense", DateTime.UtcNow.Month, 1500m);
            await RecordSpentAsync(seedClient, budgetId, leafId, itemId, 1000m);
        }

        await using BudgetTestWebApplicationFactory factory = new(ItemsFeatureOffHousehold)
        {
            MenloConnectionString = fixture.ConnectionString,
            SkipMigration = true,
            ConfigurationOverrides = new Dictionary<string, string?>
            {
                ["Features:Budget"] = "true",
                ["Features:BudgetItems"] = "false"
            }
        };
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: TestContext.Current.CancellationToken);

        HttpResponseMessage response = await client.GetAsync("/api/budgets", TestContext.Current.CancellationToken);
        List<BudgetListItem>? budgets = await DeserializeAsync(response);

        ItShouldHaveReturned200Ok(response);
        budgets.ShouldNotBeNull();
        BudgetListItem budget = budgets.ShouldHaveSingleItem();
        budget.PlannedExpenses.ShouldBe(new MoneyDto(0m, "ZAR"));
        budget.SpentExpenses.ShouldBeNull();
        budget.CategoryCount.ShouldBe(2);
    }

    // -------------------------------------------------------------------------
    // Standalone factory tests — no DB needed
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GivenUnauthenticatedUser_WhenListBudgets_ThenReturns401()
    {
        await using TestWebApplicationFactory factory = new()
        {
            SimulateUnauthenticated = true,
            MenloConnectionString = null
        };
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: TestContext.Current.CancellationToken);

        HttpResponseMessage response = await client.GetAsync("/api/budgets", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GivenFeatureToggleOff_WhenListBudgets_ThenReturns404()
    {
        await using TestWebApplicationFactory factory = new()
        {
            ConfigurationOverrides = new Dictionary<string, string?>
            {
                ["Features:Budget"] = "false"
            }
        };
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: TestContext.Current.CancellationToken);

        HttpResponseMessage response = await client.GetAsync("/api/budgets", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

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

    private static async Task<Guid> CreateBudgetAsync(HttpClient client, int year)
    {
        HttpResponseMessage response = await client.PostAsync(
            $"/api/budgets/{year}", null, TestContext.Current.CancellationToken);
        response.IsSuccessStatusCode.ShouldBeTrue(
            $"Budget creation failed: {await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)}");

        string content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        return JsonDocument.Parse(content).RootElement.GetProperty("id").GetGuid();
    }

    private static async Task<(Guid RootId, Guid LeafId)> CreateRootAndLeafAsync(
        HttpClient client, Guid budgetId, string name, string budgetFlow)
    {
        HttpResponseMessage rootResponse = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories",
            new CreateCategoryRequest(name, budgetFlow),
            TestContext.Current.CancellationToken);
        rootResponse.IsSuccessStatusCode.ShouldBeTrue(
            $"Root category creation failed: {await rootResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)}");
        string rootContent = await rootResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Guid rootId = JsonDocument.Parse(rootContent).RootElement.GetProperty("id").GetGuid();

        HttpResponseMessage leafResponse = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories",
            new CreateCategoryRequest($"{name} leaf", budgetFlow, ParentId: rootId),
            TestContext.Current.CancellationToken);
        leafResponse.IsSuccessStatusCode.ShouldBeTrue(
            $"Leaf category creation failed: {await leafResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)}");
        string leafContent = await leafResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        return (rootId, JsonDocument.Parse(leafContent).RootElement.GetProperty("id").GetGuid());
    }

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

    private static async Task RecordSpentAsync(
        HttpClient client, Guid budgetId, Guid categoryId, Guid itemId, decimal amount)
    {
        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items/{itemId}/spent",
            new RecordBudgetItemSpentRequest(amount),
            TestContext.Current.CancellationToken);
        response.IsSuccessStatusCode.ShouldBeTrue(
            $"Recording spent failed: {await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)}");
    }

    private static async Task<List<BudgetListItem>?> DeserializeAsync(HttpResponseMessage response)
    {
        string content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        return JsonSerializer.Deserialize<List<BudgetListItem>>(content, JsonOptions);
    }

    // -------------------------------------------------------------------------
    // Assertion helpers
    // -------------------------------------------------------------------------

    private static void ItShouldHaveReturned200Ok(HttpResponseMessage response) =>
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

    private static void ItShouldListTheBudgetsNewestFirst(
        List<BudgetListItem>? budgets, Guid nextYearId, Guid currentYearId)
    {
        budgets.ShouldNotBeNull();
        budgets.Select(b => b.Id).ShouldBe([nextYearId, currentYearId]);
    }

    private static void ItShouldHaveNoExpensesForNextYear(BudgetListItem budget)
    {
        budget.PlannedExpenses.ShouldBe(new MoneyDto(0m, "ZAR"));
        budget.SpentExpenses.ShouldBeNull();
        budget.CategoryCount.ShouldBe(0);
    }
}
