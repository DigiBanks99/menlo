using Menlo.Api.Budget;
using Menlo.Api.Tests.Fixtures;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Menlo.Api.Tests.Budget;

/// <summary>
/// <c>POST /api/budgets/{year}/categories</c>, and the category tree in <c>GET /api/budgets/{year}</c>, against a real
/// PostgreSQL database.
/// </summary>
[ClassDataSource<PostgresApiFixture>(Shared = SharedType.PerTestSession)]
public sealed class BudgetCategoryEndpointTests(PostgresApiFixture fixture) : TestFixture
{
    private static readonly Dictionary<string, string?> BudgetOn = new() { ["Features:Budget"] = "true" };
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    // The database is shared by the whole test session and holds one budget per year, so each test takes years no
    // other test uses
    private static int _lastYear = Random.Shared.Next(6000, 9000);

    private static CancellationToken Cancellation => TestContext.Current!.Execution.CancellationToken;

    private static int UnusedYear() => Interlocked.Increment(ref _lastYear);

    [Test]
    public async Task AMemberAddsATopLevelCategory()
    {
        await using PostgresTestWebApplicationFactory factory = fixture.CreateFactory(BudgetOn);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: Cancellation);
        int year = await CreateBudgetAsync(client);

        HttpResponseMessage response = await AddAsync(client, year, new AddCategoryRequest("Home"));
        CategoryDto? category = await response.Content.ReadFromJsonAsync<CategoryDto>(Json, Cancellation);

        ItShouldBeCreated();
        ItShouldPointToTheCategory();
        ItShouldBeATopLevelCategory();

        void ItShouldBeCreated() => response.StatusCode.ShouldBe(HttpStatusCode.Created);

        void ItShouldPointToTheCategory() => response.Headers.Location
            .ShouldBe(new Uri($"/api/budgets/{year}/categories/{category!.Id}", UriKind.Relative));

        void ItShouldBeATopLevelCategory() => category.ShouldNotBeNull().ShouldSatisfyAllConditions(
            c => c.Id.ShouldNotBe(Guid.Empty),
            c => c.Name.ShouldBe("Home"),
            c => c.ParentId.ShouldBeNull(),
            c => c.Children.ShouldBeEmpty());
    }

    [Test]
    public async Task AMemberBuildsACategoryTree()
    {
        await using PostgresTestWebApplicationFactory factory = fixture.CreateFactory(BudgetOn);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: Cancellation);
        int year = await CreateBudgetAsync(client);

        CategoryDto school = await AddCategoryAsync(client, year, new AddCategoryRequest("School"));
        CategoryDto home = await AddCategoryAsync(client, year, new AddCategoryRequest("Home"));
        CategoryDto water = await AddCategoryAsync(client, year, new AddCategoryRequest("Water", home.Id));
        CategoryDto electricity = await AddCategoryAsync(client, year, new AddCategoryRequest("electricity", home.Id));
        BudgetDto? budget = await client.GetFromJsonAsync<BudgetDto>($"/api/budgets/{year}", Json, Cancellation);

        ItShouldPutTheSubcategoryUnderItsParent();
        ItShouldListTheTopLevelCategoriesByName();
        ItShouldListEachCategorysChildrenByName();

        void ItShouldPutTheSubcategoryUnderItsParent() => water.ParentId.ShouldBe(home.Id);

        void ItShouldListTheTopLevelCategoriesByName() => budget.ShouldNotBeNull().Categories
            .Select(c => c.Id).ShouldBe([home.Id, school.Id]);

        void ItShouldListEachCategorysChildrenByName() => budget!.Categories.ShouldSatisfyAllConditions(
            categories => categories[0].Children.ShouldBe(
                [new CategoryDto(electricity.Id, "electricity", home.Id, []), new CategoryDto(water.Id, "Water", home.Id, [])],
                new CategoryComparer()),
            categories => categories[1].Children.ShouldBeEmpty());
    }

    [Test]
    public async Task AMemberAddsACategoryUnderASubcategory()
    {
        await using PostgresTestWebApplicationFactory factory = fixture.CreateFactory(BudgetOn);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: Cancellation);
        int year = await CreateBudgetAsync(client);
        CategoryDto home = await AddCategoryAsync(client, year, new AddCategoryRequest("Home"));
        CategoryDto electricity = await AddCategoryAsync(client, year, new AddCategoryRequest("Electricity", home.Id));

        HttpResponseMessage response = await AddAsync(client, year, new AddCategoryRequest("Prepaid", electricity.Id));
        using JsonDocument problem = await ReadProblemAsync(response);

        ItShouldBeRefusedAsAThirdLevel();
        await ItShouldLeaveTheTreeTwoDeep();

        void ItShouldBeRefusedAsAThirdLevel() => response.ShouldSatisfyAllConditions(
            r => r.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity),
            _ => problem.RootElement.GetProperty("code").GetString().ShouldBe("Budget.CategoryTooDeep"));

        async Task ItShouldLeaveTheTreeTwoDeep() => (await GetBudgetAsync(client, year)).Categories
            .ShouldHaveSingleItem().Children.ShouldHaveSingleItem().Children.ShouldBeEmpty();
    }

    [Test]
    public async Task AMemberAddsACategoryUnderOneTheBudgetDoesNotHave()
    {
        await using PostgresTestWebApplicationFactory factory = fixture.CreateFactory(BudgetOn);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: Cancellation);
        int year = await CreateBudgetAsync(client);
        int otherYear = await CreateBudgetAsync(client);
        CategoryDto elsewhere = await AddCategoryAsync(client, otherYear, new AddCategoryRequest("Home"));

        HttpResponseMessage unknown = await AddAsync(client, year, new AddCategoryRequest("Electricity", Guid.NewGuid()));
        using JsonDocument unknownProblem = await ReadProblemAsync(unknown);
        HttpResponseMessage otherBudgets = await AddAsync(client, year, new AddCategoryRequest("Electricity", elsewhere.Id));

        ItShouldRefuseAnUnknownParent();
        ItShouldRefuseAnotherBudgetsCategory();

        void ItShouldRefuseAnUnknownParent() => unknown.ShouldSatisfyAllConditions(
            r => r.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity),
            _ => unknownProblem.RootElement.GetProperty("code").GetString().ShouldBe("Budget.CategoryNotFound"));

        void ItShouldRefuseAnotherBudgetsCategory() => otherBudgets.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [Test]
    public async Task AMemberAddsACategoryWithASiblingsName()
    {
        await using PostgresTestWebApplicationFactory factory = fixture.CreateFactory(BudgetOn);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: Cancellation);
        int year = await CreateBudgetAsync(client);
        CategoryDto home = await AddCategoryAsync(client, year, new AddCategoryRequest("Home"));
        await AddCategoryAsync(client, year, new AddCategoryRequest("Water", home.Id));

        HttpResponseMessage topLevel = await AddAsync(client, year, new AddCategoryRequest("HOME"));
        using JsonDocument problem = await ReadProblemAsync(topLevel);
        HttpResponseMessage child = await AddAsync(client, year, new AddCategoryRequest("water", home.Id));
        HttpResponseMessage elsewhere = await AddAsync(client, year, new AddCategoryRequest("Water"));

        ItShouldRefuseATakenTopLevelName();
        ItShouldRefuseATakenChildName();
        ItShouldAllowTheNameElsewhere();

        void ItShouldRefuseATakenTopLevelName() => topLevel.ShouldSatisfyAllConditions(
            r => r.StatusCode.ShouldBe(HttpStatusCode.Conflict),
            _ => problem.RootElement.GetProperty("code").GetString().ShouldBe("Budget.CategoryNameTaken"),
            _ => problem.RootElement.GetProperty("detail").GetString().ShouldBe("There is already a category called \"HOME\" here."));

        void ItShouldRefuseATakenChildName() => child.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        void ItShouldAllowTheNameElsewhere() => elsewhere.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Test]
    [Arguments("")]
    [Arguments("   ")]
    [Arguments(null)]
    public async Task AMemberAddsACategoryWithoutAName(string? name)
    {
        await using PostgresTestWebApplicationFactory factory = fixture.CreateFactory(BudgetOn);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: Cancellation);
        int year = await CreateBudgetAsync(client);

        HttpResponseMessage response = await AddAsync(client, year, new AddCategoryRequest(name));
        using JsonDocument problem = await ReadProblemAsync(response);

        ItShouldBeRefused();
        await ItShouldAddNothing();

        void ItShouldBeRefused() => response.ShouldSatisfyAllConditions(
            r => r.StatusCode.ShouldBe(HttpStatusCode.BadRequest),
            _ => problem.RootElement.GetProperty("code").GetString().ShouldBe("Budget.InvalidCategoryName"));

        async Task ItShouldAddNothing() => (await GetBudgetAsync(client, year)).Categories.ShouldBeEmpty();
    }

    [Test]
    public async Task AMemberAddsACategoryWithANameTooLong()
    {
        await using PostgresTestWebApplicationFactory factory = fixture.CreateFactory(BudgetOn);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: Cancellation);
        int year = await CreateBudgetAsync(client);

        HttpResponseMessage response = await AddAsync(client, year, new AddCategoryRequest(new string('a', 101)));

        ItShouldBeRefused();

        void ItShouldBeRefused() => response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task AMemberAddsACategoryToAYearThatHasNoBudget()
    {
        await using PostgresTestWebApplicationFactory factory = fixture.CreateFactory(BudgetOn);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: Cancellation);
        int year = UnusedYear();

        HttpResponseMessage response = await AddAsync(client, year, new AddCategoryRequest("Home"));
        using JsonDocument problem = await ReadProblemAsync(response);

        ItShouldFindNoBudget();

        void ItShouldFindNoBudget() => response.ShouldSatisfyAllConditions(
            r => r.StatusCode.ShouldBe(HttpStatusCode.NotFound),
            _ => problem.RootElement.GetProperty("code").GetString().ShouldBe("Budget.NotFound"));
    }

    [Test]
    public async Task AMemberAddsACategoryWithoutABody()
    {
        await using PostgresTestWebApplicationFactory factory = fixture.CreateFactory(BudgetOn);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: Cancellation);
        int year = await CreateBudgetAsync(client);

        HttpResponseMessage response = await client.PostAsync(
            $"/api/budgets/{year}/categories",
            new StringContent("", System.Text.Encoding.UTF8, "application/json"),
            Cancellation);

        ItShouldBeRefused();

        void ItShouldBeRefused() => response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task TheBudgetIsSwitchedOff()
    {
        await using PostgresTestWebApplicationFactory on = fixture.CreateFactory(BudgetOn);
        using HttpClient onClient = await on.CreateAntiforgeryClientAsync(cancellationToken: Cancellation);
        int year = await CreateBudgetAsync(onClient);
        await using PostgresTestWebApplicationFactory off = fixture.CreateFactory(new Dictionary<string, string?> { ["Features:Budget"] = "false" });
        using HttpClient offClient = await off.CreateAntiforgeryClientAsync(cancellationToken: Cancellation);

        HttpResponseMessage response = await AddAsync(offClient, year, new AddCategoryRequest("Home"));

        ItShouldNotAddACategory();
        await ItShouldLeaveTheBudgetEmpty();

        void ItShouldNotAddACategory() => response.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        async Task ItShouldLeaveTheBudgetEmpty() => (await GetBudgetAsync(onClient, year)).Categories.ShouldBeEmpty();
    }

    [Test]
    public async Task SomeoneWhoIsNotSignedInAddsACategory()
    {
        await using TestWebApplicationFactory factory = new() { SimulateUnauthenticated = true, ConfigurationOverrides = BudgetOn };
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: Cancellation);

        HttpResponseMessage response = await AddAsync(client, 2027, new AddCategoryRequest("Home"));

        ItShouldTurnThemAway();

        void ItShouldTurnThemAway() => response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private static async Task<int> CreateBudgetAsync(HttpClient client)
    {
        int year = UnusedYear();
        (await client.PostAsJsonAsync("/api/budgets", new CreateBudgetRequest(year), Cancellation)).EnsureSuccessStatusCode();
        return year;
    }

    private static Task<HttpResponseMessage> AddAsync(HttpClient client, int year, AddCategoryRequest request) =>
        client.PostAsJsonAsync($"/api/budgets/{year}/categories", request, Cancellation);

    private static async Task<CategoryDto> AddCategoryAsync(HttpClient client, int year, AddCategoryRequest request)
    {
        HttpResponseMessage response = await AddAsync(client, year, request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CategoryDto>(Json, Cancellation)).ShouldNotBeNull();
    }

    private static async Task<BudgetDto> GetBudgetAsync(HttpClient client, int year) =>
        (await client.GetFromJsonAsync<BudgetDto>($"/api/budgets/{year}", Json, Cancellation)).ShouldNotBeNull();

    private static async Task<JsonDocument> ReadProblemAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync(Cancellation));

    // Records with list members compare their lists by reference, so categories are compared field by field
    private sealed class CategoryComparer : IEqualityComparer<CategoryDto>
    {
        public bool Equals(CategoryDto? x, CategoryDto? y) =>
            x is not null && y is not null && x.Id == y.Id && x.Name == y.Name && x.ParentId == y.ParentId
            && x.Children.SequenceEqual(y.Children, this);

        public int GetHashCode(CategoryDto obj) => obj.Id.GetHashCode();
    }
}
