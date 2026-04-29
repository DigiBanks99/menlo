using Menlo.Api.Budget;
using Menlo.Application.Common;
using Menlo.Lib.Budget.Entities;
using Menlo.Lib.Budget.Enums;
using Menlo.Lib.Budget.ValueObjects;
using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Common.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Menlo.Api.Tests.Budget;

[Collection("Budget")]
public sealed class ActivateBudgetEndpointTests(BudgetApiFixture fixture) : TestFixture
{
    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNameCaseInsensitive = true };

    // Dedicated HouseholdIds — never used by other test classes
    private static readonly HouseholdId HappyPathHousehold =
        new(Guid.Parse("f0f0f0f0-f0f0-f0f0-f0f0-f0f0f0f0f0f0"));

    private static readonly HouseholdId NoNonZeroAmountsHousehold =
        new(Guid.Parse("f1f1f1f1-f1f1-f1f1-f1f1-f1f1f1f1f1f1"));

    private static readonly HouseholdId AlreadyActiveHousehold =
        new(Guid.Parse("f2f2f2f2-f2f2-f2f2-f2f2-f2f2f2f2f2f2"));

    private static readonly HouseholdId AutoClosePreviousYearHousehold =
        new(Guid.Parse("f3f3f3f3-f3f3-f3f3-f3f3-f3f3f3f3f3f3"));

    private static readonly HouseholdId WrongHouseholdOwner =
        new(Guid.Parse("f4f4f4f4-f4f4-f4f4-f4f4-f4f4f4f4f4f4"));

    // -------------------------------------------------------------------------
    // Happy path
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GivenDraftBudgetWithNonZeroAmount_WhenActivate_ThenReturns200WithActiveBudget()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(HappyPathHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: TestContext.Current.CancellationToken);
        int year = DateTimeOffset.UtcNow.Year;

        HttpResponseMessage createResponse =
            await client.PostAsync($"/api/budgets/{year}", null, TestContext.Current.CancellationToken);
        BudgetDto? createdDto = await DeserializeBudgetDto(createResponse);
        createResponse.IsSuccessStatusCode.ShouldBeTrue();
        createdDto.ShouldNotBeNull();

        await SeedNonZeroCategoryAsync(factory, createdDto.Id);

        HttpResponseMessage activateResponse =
            await client.PostAsync($"/api/budgets/{createdDto.Id}/activate", null, TestContext.Current.CancellationToken);
        BudgetDto? dto = await DeserializeBudgetDto(activateResponse);

        ItShouldHaveReturned200Ok(activateResponse);
        ItShouldHaveActiveStatus(dto);
        ItShouldHaveMatchingId(dto, createdDto.Id);
        ItShouldHaveTheCorrectYear(dto, year);
        ItShouldBelongToHousehold(dto, HappyPathHousehold);
    }

    // -------------------------------------------------------------------------
    // No non-zero amounts
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GivenDraftBudgetWithNoNonZeroAmounts_WhenActivate_ThenReturns200()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(NoNonZeroAmountsHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: TestContext.Current.CancellationToken);
        int year = DateTimeOffset.UtcNow.Year;

        HttpResponseMessage createResponse =
            await client.PostAsync($"/api/budgets/{year}", null, TestContext.Current.CancellationToken);
        BudgetDto? createdDto = await DeserializeBudgetDto(createResponse);
        createResponse.IsSuccessStatusCode.ShouldBeTrue();
        createdDto.ShouldNotBeNull();

        HttpResponseMessage activateResponse =
            await client.PostAsync($"/api/budgets/{createdDto.Id}/activate", null, TestContext.Current.CancellationToken);

        ItShouldHaveReturned200Ok(activateResponse);
    }

    // -------------------------------------------------------------------------
    // Already active
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GivenAlreadyActiveBudget_WhenActivateAgain_ThenReturns400()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(AlreadyActiveHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: TestContext.Current.CancellationToken);
        int year = DateTimeOffset.UtcNow.Year;

        HttpResponseMessage createResponse =
            await client.PostAsync($"/api/budgets/{year}", null, TestContext.Current.CancellationToken);
        BudgetDto? createdDto = await DeserializeBudgetDto(createResponse);
        createResponse.IsSuccessStatusCode.ShouldBeTrue();
        createdDto.ShouldNotBeNull();

        await SeedNonZeroCategoryAsync(factory, createdDto.Id);

        HttpResponseMessage firstActivateResponse =
            await client.PostAsync($"/api/budgets/{createdDto.Id}/activate", null, TestContext.Current.CancellationToken);
        firstActivateResponse.IsSuccessStatusCode.ShouldBeTrue();

        HttpResponseMessage secondActivateResponse =
            await client.PostAsync($"/api/budgets/{createdDto.Id}/activate", null, TestContext.Current.CancellationToken);

        ItShouldHaveReturned400BadRequest(secondActivateResponse);
    }

    // -------------------------------------------------------------------------
    // Auto-close previous year
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GivenPreviousYearActiveBudget_WhenActivateCurrentYear_ThenPreviousYearBudgetIsClosed()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(AutoClosePreviousYearHousehold);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: TestContext.Current.CancellationToken);
        int currentYear = DateTimeOffset.UtcNow.Year;
        int previousYear = currentYear - 1;

        // Seed previous year active budget directly via DB
        Guid previousYearBudgetId = await SeedActiveBudgetForYearAsync(factory, AutoClosePreviousYearHousehold, previousYear);

        // Create and activate current year budget via API
        HttpResponseMessage createResponse =
            await client.PostAsync($"/api/budgets/{currentYear}", null, TestContext.Current.CancellationToken);
        BudgetDto? createdDto = await DeserializeBudgetDto(createResponse);
        createResponse.IsSuccessStatusCode.ShouldBeTrue();
        createdDto.ShouldNotBeNull();

        await SeedNonZeroCategoryAsync(factory, createdDto.Id);

        HttpResponseMessage activateResponse =
            await client.PostAsync($"/api/budgets/{createdDto.Id}/activate", null, TestContext.Current.CancellationToken);

        // Re-query previous year budget in a new scope to avoid tracking conflicts
        string closedStatus = await GetBudgetStatusFromDbAsync(factory, previousYearBudgetId);

        ItShouldHaveReturned200Ok(activateResponse);
        ItShouldHavePreviousYearBudgetClosed(closedStatus);
    }

    // -------------------------------------------------------------------------
    // Not found scenarios
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GivenUnknownBudgetId_WhenActivate_ThenReturns404()
    {
        using HttpClient client = await fixture.CreateAntiforgeryClientAsync(TestContext.Current.CancellationToken);
        Guid unknownId = Guid.NewGuid();

        HttpResponseMessage response =
            await client.PostAsync($"/api/budgets/{unknownId}/activate", null, TestContext.Current.CancellationToken);

        ItShouldHaveReturned404NotFound(response);
    }

    [Fact]
    public async Task GivenBudgetBelongingToDifferentHousehold_WhenActivate_ThenReturns404()
    {
        // Create budget under isolated household
        await using BudgetTestWebApplicationFactory ownerFactory = CreateIsolatedFactory(WrongHouseholdOwner);
        using HttpClient ownerClient = await ownerFactory.CreateAntiforgeryClientAsync(cancellationToken: TestContext.Current.CancellationToken);
        int year = DateTimeOffset.UtcNow.Year;

        HttpResponseMessage createResponse =
            await ownerClient.PostAsync($"/api/budgets/{year}", null, TestContext.Current.CancellationToken);
        BudgetDto? createdDto = await DeserializeBudgetDto(createResponse);
        createResponse.IsSuccessStatusCode.ShouldBeTrue();
        createdDto.ShouldNotBeNull();

        // Try to activate using the shared fixture client (different household)
        using HttpClient otherClient = await fixture.CreateAntiforgeryClientAsync(TestContext.Current.CancellationToken);

        HttpResponseMessage activateResponse =
            await otherClient.PostAsync($"/api/budgets/{createdDto.Id}/activate", null, TestContext.Current.CancellationToken);

        ItShouldHaveReturned404NotFound(activateResponse);
    }

    // -------------------------------------------------------------------------
    // Auth / feature flag
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GivenUnauthenticatedUser_WhenActivate_ThenReturns401()
    {
        await using TestWebApplicationFactory factory = new()
        {
            SimulateUnauthenticated = true,
            MenloConnectionString = null
        };
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: TestContext.Current.CancellationToken);

        HttpResponseMessage response =
            await client.PostAsync($"/api/budgets/{Guid.NewGuid()}/activate", null, TestContext.Current.CancellationToken);

        ItShouldHaveBeenUnauthorised(response);
    }

    [Fact]
    public async Task GivenFeatureToggleOff_WhenActivate_ThenReturns404()
    {
        await using TestWebApplicationFactory factory = new()
        {
            ConfigurationOverrides = new Dictionary<string, string?>
            {
                ["Features:Budget"] = "false"
            }
        };
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: TestContext.Current.CancellationToken);

        HttpResponseMessage response =
            await client.PostAsync($"/api/budgets/{Guid.NewGuid()}/activate", null, TestContext.Current.CancellationToken);

        ItShouldHaveReturned404NotFound(response);
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

    private static async Task SeedNonZeroCategoryAsync(BudgetTestWebApplicationFactory factory, Guid budgetId)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        MenloDbContext db = scope.ServiceProvider.GetRequiredService<MenloDbContext>();
        IAuditStampFactory auditFactory = scope.ServiceProvider.GetRequiredService<IAuditStampFactory>();

        Lib.Budget.Entities.Budget budget = await db.Budgets
            .Include(b => b.Categories)
            .FirstAsync(b => b.Id == new BudgetId(budgetId), TestContext.Current.CancellationToken);

        budget.AddCategory("Housing", BudgetFlow.Expense);
        CategoryNode category = budget.Categories.First(c => c.Name.Value == "Housing");

        CanonicalCategory canonical = CanonicalCategory.Create(category.CanonicalCategoryId, category.Name.Value);
        canonical.Audit(auditFactory, Menlo.Lib.Common.Enums.AuditOperation.Create);
        db.CanonicalCategories.Add(canonical);

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<Guid> SeedActiveBudgetForYearAsync(
        BudgetTestWebApplicationFactory factory,
        HouseholdId householdId,
        int year)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        MenloDbContext db = scope.ServiceProvider.GetRequiredService<MenloDbContext>();
        IAuditStampFactory auditFactory = scope.ServiceProvider.GetRequiredService<IAuditStampFactory>();

        Lib.Budget.Entities.Budget budget = Lib.Budget.Entities.Budget
            .Create(householdId, year, auditFactory)
            .Value;

        budget.AddCategory("Housing", BudgetFlow.Expense);
        CategoryNode category = budget.Categories.First(c => c.Name.Value == "Housing");

        CanonicalCategory canonical = CanonicalCategory.Create(category.CanonicalCategoryId, category.Name.Value);
        canonical.Audit(auditFactory, Menlo.Lib.Common.Enums.AuditOperation.Create);
        db.CanonicalCategories.Add(canonical);

        budget.Activate();

        db.Budgets.Add(budget);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        return budget.Id.Value;
    }

    private static async Task<string> GetBudgetStatusFromDbAsync(
        BudgetTestWebApplicationFactory factory,
        Guid budgetId)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        MenloDbContext db = scope.ServiceProvider.GetRequiredService<MenloDbContext>();

        Lib.Budget.Entities.Budget budget = await db.Budgets
            .AsNoTracking()
            .FirstAsync(b => b.Id == new BudgetId(budgetId), TestContext.Current.CancellationToken);

        return budget.Status.ToString();
    }

    // -------------------------------------------------------------------------
    // Assertion helpers
    // -------------------------------------------------------------------------

    private static void ItShouldHaveReturned200Ok(HttpResponseMessage response) =>
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

    private static void ItShouldHaveReturned400BadRequest(HttpResponseMessage response) =>
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

    private static void ItShouldHaveReturned404NotFound(HttpResponseMessage response) =>
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);

    private static void ItShouldHaveActiveStatus(BudgetDto? dto)
    {
        dto.ShouldNotBeNull();
        dto.Status.ShouldBe("Active");
    }

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

    private static void ItShouldBelongToHousehold(BudgetDto? dto, HouseholdId expectedHouseholdId)
    {
        dto.ShouldNotBeNull();
        dto.HouseholdId.ShouldBe(expectedHouseholdId.Value);
    }

    private static void ItShouldHavePreviousYearBudgetClosed(string status) =>
        status.ShouldBe("Closed");

    private static async Task<BudgetDto?> DeserializeBudgetDto(HttpResponseMessage response)
    {
        string content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<BudgetDto>(content, JsonOptions);
    }
}
