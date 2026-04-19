using Menlo.Api.Budget;
using Menlo.Application.Common;
using Menlo.Lib.Budget.Entities;
using Menlo.Lib.Budget.ValueObjects;
using Menlo.Lib.Common.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.Json;

namespace Menlo.Api.Tests.Budget;

[Collection("Budget")]
public sealed class CloneBudgetEndpointTests(BudgetApiFixture fixture) : TestFixture
{
    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNameCaseInsensitive = true };

    private static readonly HouseholdId CloneWithCategoriesHousehold =
        new(Guid.Parse("70707070-7070-7070-7070-707070707070"));

    private static readonly HouseholdId NoCurrentYearHousehold =
        new(Guid.Parse("71717171-7171-7171-7171-717171717171"));

    private static readonly HouseholdId NestedCategoriesHousehold =
        new(Guid.Parse("72727272-7272-7272-7272-727272727272"));

    // -------------------------------------------------------------------------
    // Clone with categories
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GivenCurrentYearBudgetExists_WhenCreateNextYearBudget_ThenReturns201WithClonedCategories()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(CloneWithCategoriesHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: TestContext.Current.CancellationToken);
        int currentYear = DateTimeOffset.UtcNow.Year;

        // Create current-year budget via API
        HttpResponseMessage createResponse =
            await client.PostAsync($"/api/budgets/{currentYear}", null, TestContext.Current.CancellationToken);
        BudgetDto? createdDto = await DeserializeBudgetDto(createResponse);
        createResponse.IsSuccessStatusCode.ShouldBeTrue();
        createdDto.ShouldNotBeNull();

        // Seed a category with a planned amount
        Guid sourceCategoryId = await SeedCategoryWithPlannedAmountAsync(factory, createdDto.Id, "Housing", 1000m, "ZAR");

        // Create next-year budget via API (should clone)
        HttpResponseMessage response =
            await client.PostAsync($"/api/budgets/{currentYear + 1}", null, TestContext.Current.CancellationToken);
        BudgetDto? dto = await DeserializeBudgetDto(response);

        ItShouldHaveReturned201Created(response);
        ItShouldHaveYear(dto, currentYear + 1);
        ItShouldHaveDraftStatus(dto);
        ItShouldHaveExactlyOneCategory(dto);
        ItShouldHaveCategoryNamed(dto, "Housing");
        ItShouldHaveCategoryPlannedAmount(dto, 1000m, "ZAR");
        ItShouldHaveNewCategoryId(dto, sourceCategoryId);
    }

    // -------------------------------------------------------------------------
    // No current-year budget → empty clone fallback
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GivenNoCurrentYearBudget_WhenCreateNextYearBudget_ThenReturns201WithEmptyCategories()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(NoCurrentYearHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: TestContext.Current.CancellationToken);
        int currentYear = DateTimeOffset.UtcNow.Year;

        // Do NOT create any current-year budget — go straight to next year
        HttpResponseMessage response =
            await client.PostAsync($"/api/budgets/{currentYear + 1}", null, TestContext.Current.CancellationToken);
        BudgetDto? dto = await DeserializeBudgetDto(response);

        ItShouldHaveReturned201Created(response);
        ItShouldHaveYear(dto, currentYear + 1);
        ItShouldHaveNoCategories(dto);
    }

    // -------------------------------------------------------------------------
    // Nested categories — parent/child relationships preserved after clone
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GivenNestedCategoriesInCurrentYear_WhenCreateNextYearBudget_ThenParentChildRelationshipsPreserved()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(NestedCategoriesHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: TestContext.Current.CancellationToken);
        int currentYear = DateTimeOffset.UtcNow.Year;

        // Create current-year budget via API
        HttpResponseMessage createResponse =
            await client.PostAsync($"/api/budgets/{currentYear}", null, TestContext.Current.CancellationToken);
        BudgetDto? createdDto = await DeserializeBudgetDto(createResponse);
        createResponse.IsSuccessStatusCode.ShouldBeTrue();
        createdDto.ShouldNotBeNull();

        // Seed parent + child categories
        await SeedNestedCategoriesAsync(factory, createdDto.Id, "Expenses", "Groceries");

        // Create next-year budget via API (should clone with hierarchy)
        HttpResponseMessage response =
            await client.PostAsync($"/api/budgets/{currentYear + 1}", null, TestContext.Current.CancellationToken);
        BudgetDto? dto = await DeserializeBudgetDto(response);

        ItShouldHaveReturned201Created(response);
        ItShouldHaveExactlyTwoCategories(dto);
        ItShouldHaveParentCategoryWithNullParentId(dto, "Expenses");
        ItShouldHaveChildCategoryLinkedToClonedParent(dto, "Expenses", "Groceries");
    }

    // -------------------------------------------------------------------------
    // Factory helper
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

    // -------------------------------------------------------------------------
    // DB seeding helpers
    // -------------------------------------------------------------------------

    private static async Task<Guid> SeedCategoryWithPlannedAmountAsync(
        BudgetTestWebApplicationFactory factory,
        Guid budgetId,
        string categoryName,
        decimal amount,
        string currency)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        MenloDbContext db = scope.ServiceProvider.GetRequiredService<MenloDbContext>();

        Lib.Budget.Entities.Budget budget = await db.Budgets
            .Include(b => b.Categories)
            .FirstAsync(b => b.Id == new BudgetId(budgetId), TestContext.Current.CancellationToken);

        budget.AddCategory(categoryName);
        CategoryNode category = budget.Categories.First();
        Money money = Money.Create(amount, currency).Value;
        budget.SetPlanned(category.Id, money);

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        return category.Id.Value;
    }

    private static async Task SeedNestedCategoriesAsync(
        BudgetTestWebApplicationFactory factory,
        Guid budgetId,
        string parentName,
        string childName)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        MenloDbContext db = scope.ServiceProvider.GetRequiredService<MenloDbContext>();

        Lib.Budget.Entities.Budget budget = await db.Budgets
            .Include(b => b.Categories)
            .FirstAsync(b => b.Id == new BudgetId(budgetId), TestContext.Current.CancellationToken);

        budget.AddCategory(parentName);
        CategoryNode parent = budget.Categories.First(c => c.Name.Value == parentName);

        budget.AddCategory(childName, parent.Id);

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    // -------------------------------------------------------------------------
    // Deserialisation helper
    // -------------------------------------------------------------------------

    private static async Task<BudgetDto?> DeserializeBudgetDto(HttpResponseMessage response)
    {
        string content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<BudgetDto>(content, JsonOptions);
    }

    // -------------------------------------------------------------------------
    // Assertion helpers
    // -------------------------------------------------------------------------

    private static void ItShouldHaveReturned201Created(HttpResponseMessage response) =>
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

    private static void ItShouldHaveYear(BudgetDto? dto, int expectedYear)
    {
        dto.ShouldNotBeNull();
        dto.Year.ShouldBe(expectedYear);
    }

    private static void ItShouldHaveDraftStatus(BudgetDto? dto)
    {
        dto.ShouldNotBeNull();
        dto.Status.ShouldBe("Draft");
    }

    private static void ItShouldHaveExactlyOneCategory(BudgetDto? dto)
    {
        dto.ShouldNotBeNull();
        dto.Categories.Count.ShouldBe(1);
    }

    private static void ItShouldHaveExactlyTwoCategories(BudgetDto? dto)
    {
        dto.ShouldNotBeNull();
        dto.Categories.Count.ShouldBe(2);
    }

    private static void ItShouldHaveNoCategories(BudgetDto? dto)
    {
        dto.ShouldNotBeNull();
        dto.Categories.ShouldBeEmpty();
    }

    private static void ItShouldHaveCategoryNamed(BudgetDto? dto, string expectedName)
    {
        dto.ShouldNotBeNull();
        dto.Categories.ShouldContain(c => c.Name == expectedName);
    }

    private static void ItShouldHaveCategoryPlannedAmount(BudgetDto? dto, decimal expectedAmount, string expectedCurrency)
    {
        dto.ShouldNotBeNull();
        CategoryNodeDto category = dto.Categories.ShouldHaveSingleItem();
        category.PlannedMonthlyAmount.Amount.ShouldBe(expectedAmount);
        category.PlannedMonthlyAmount.Currency.ShouldBe(expectedCurrency);
    }

    private static void ItShouldHaveNewCategoryId(BudgetDto? dto, Guid sourceCategoryId)
    {
        dto.ShouldNotBeNull();
        CategoryNodeDto clonedCategory = dto.Categories.ShouldHaveSingleItem();
        clonedCategory.Id.ShouldNotBe(sourceCategoryId);
    }

    private static void ItShouldHaveParentCategoryWithNullParentId(BudgetDto? dto, string parentName)
    {
        dto.ShouldNotBeNull();
        CategoryNodeDto? parent = dto.Categories.FirstOrDefault(c => c.Name == parentName);
        parent.ShouldNotBeNull();
        parent.ParentId.ShouldBeNull();
    }

    private static void ItShouldHaveChildCategoryLinkedToClonedParent(BudgetDto? dto, string parentName, string childName)
    {
        dto.ShouldNotBeNull();

        CategoryNodeDto? clonedParent = dto.Categories.FirstOrDefault(c => c.Name == parentName);
        clonedParent.ShouldNotBeNull();

        CategoryNodeDto? child = dto.Categories.FirstOrDefault(c => c.Name == childName);
        child.ShouldNotBeNull();

        child.ParentId.ShouldBe(clonedParent.Id);
    }
}
