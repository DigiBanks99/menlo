using Menlo.Api.Budget;
using Menlo.Api.Tests.Fixtures;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Menlo.Api.Tests.Budget;

/// <summary>
/// <c>POST /api/budgets</c> and <c>GET /api/budgets/{year}</c>, against a real PostgreSQL database.
/// </summary>
[ClassDataSource<PostgresApiFixture>(Shared = SharedType.PerTestSession)]
public sealed class BudgetEndpointTests(PostgresApiFixture fixture) : TestFixture
{
    private static readonly Dictionary<string, string?> BudgetOn = new() { ["Features:Budget"] = "true" };
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    // The database is shared by the whole test session and holds one budget per year, so each test takes years no
    // other test uses
    private static int _lastYear = Random.Shared.Next(3000, 6000);

    private static CancellationToken Cancellation => TestContext.Current!.Execution.CancellationToken;

    private static int UnusedYear() => Interlocked.Increment(ref _lastYear);

    [Test]
    public async Task AMemberCreatesABudgetForAYearThatHasNone()
    {
        int year = UnusedYear();
        await using PostgresTestWebApplicationFactory factory = fixture.CreateFactory(BudgetOn);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: Cancellation);

        HttpResponseMessage created = await client.PostAsJsonAsync("/api/budgets", new CreateBudgetRequest(year), Cancellation);
        BudgetDto? budget = await created.Content.ReadFromJsonAsync<BudgetDto>(Json, Cancellation);
        HttpResponseMessage fetched = await client.GetAsync($"/api/budgets/{year}", Cancellation);
        BudgetDto? stored = await fetched.Content.ReadFromJsonAsync<BudgetDto>(Json, Cancellation);

        ItShouldBeCreated();
        ItShouldPointToTheYearsBudget();
        ItShouldCoverTheYear();
        ItShouldBeTheBudgetFetchedForTheYear();

        void ItShouldBeCreated() => created.StatusCode.ShouldBe(HttpStatusCode.Created);

        void ItShouldPointToTheYearsBudget() => created.Headers.Location.ShouldBe(new Uri($"/api/budgets/{year}", UriKind.Relative));

        void ItShouldCoverTheYear() => budget.ShouldNotBeNull().ShouldSatisfyAllConditions(
            b => b.Year.ShouldBe(year),
            b => b.Id.ShouldNotBe(Guid.Empty),
            b => b.Categories.ShouldBeEmpty());

        void ItShouldBeTheBudgetFetchedForTheYear() => fetched.ShouldSatisfyAllConditions(
            () => fetched.StatusCode.ShouldBe(HttpStatusCode.OK),
            () => stored.ShouldBeEquivalentTo(budget));
    }

    [Test]
    public async Task AMemberCreatesASecondBudgetForTheSameYear()
    {
        int year = UnusedYear();
        await using PostgresTestWebApplicationFactory factory = fixture.CreateFactory(BudgetOn);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: Cancellation);

        HttpResponseMessage first = await client.PostAsJsonAsync("/api/budgets", new CreateBudgetRequest(year), Cancellation);
        BudgetDto? kept = await first.Content.ReadFromJsonAsync<BudgetDto>(Json, Cancellation);
        HttpResponseMessage second = await client.PostAsJsonAsync("/api/budgets", new CreateBudgetRequest(year), Cancellation);
        using JsonDocument problem = await ReadProblemAsync(second);
        BudgetDto? stored = await client.GetFromJsonAsync<BudgetDto>($"/api/budgets/{year}", Json, Cancellation);

        ItShouldBeRefusedAsAConflict();
        ItShouldSayTheYearHasABudget();
        ItShouldKeepTheFirstBudget();

        void ItShouldBeRefusedAsAConflict() => second.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        void ItShouldSayTheYearHasABudget() => problem.RootElement.ShouldSatisfyAllConditions(
            p => p.GetProperty("code").GetString().ShouldBe("Budget.YearTaken"),
            p => p.GetProperty("detail").GetString().ShouldBe($"{year} already has a budget."));

        void ItShouldKeepTheFirstBudget() => stored.ShouldBeEquivalentTo(kept);
    }

    [Test]
    public async Task TwoMembersCreateTheSameYearsBudgetAtOnce()
    {
        int year = UnusedYear();
        await using PostgresTestWebApplicationFactory factory = fixture.CreateFactory(BudgetOn);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: Cancellation);

        HttpResponseMessage[] responses = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ =>
            client.PostAsJsonAsync("/api/budgets", new CreateBudgetRequest(year), Cancellation)));

        ItShouldCreateOneBudget();
        ItShouldRefuseTheRestAsConflicts();

        void ItShouldCreateOneBudget() => responses.Count(r => r.StatusCode == HttpStatusCode.Created).ShouldBe(1);

        void ItShouldRefuseTheRestAsConflicts() => responses.Count(r => r.StatusCode == HttpStatusCode.Conflict).ShouldBe(3);
    }

    [Test]
    public async Task AMemberOpensAYearThatHasNoBudget()
    {
        await using PostgresTestWebApplicationFactory factory = fixture.CreateFactory(BudgetOn);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync($"/api/budgets/{UnusedYear()}", Cancellation);

        ItShouldFindNoBudget();

        void ItShouldFindNoBudget() => response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    [Arguments(0)]
    [Arguments(-1)]
    [Arguments(10000)]
    public async Task AMemberCreatesABudgetForAYearThatCantBeABudgetYear(int year)
    {
        await using PostgresTestWebApplicationFactory factory = fixture.CreateFactory(BudgetOn);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: Cancellation);

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/budgets", new CreateBudgetRequest(year), Cancellation);
        using JsonDocument problem = await ReadProblemAsync(response);

        ItShouldBeRefused();
        ItShouldSayWhy();

        void ItShouldBeRefused() => response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        void ItShouldSayWhy() => problem.RootElement.GetProperty("code").GetString().ShouldBe("Budget.InvalidYear");
    }

    [Test]
    public async Task AMemberCreatesABudgetWithoutAYear()
    {
        await using PostgresTestWebApplicationFactory factory = fixture.CreateFactory(BudgetOn);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: Cancellation);

        HttpResponseMessage response = await client.PostAsync(
            "/api/budgets",
            new StringContent("{}", System.Text.Encoding.UTF8, "application/json"),
            Cancellation);

        ItShouldBeRefused();

        void ItShouldBeRefused() => response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task TheBudgetIsSwitchedOff()
    {
        int year = UnusedYear();
        await using PostgresTestWebApplicationFactory on = fixture.CreateFactory(BudgetOn);
        using HttpClient onClient = await on.CreateAntiforgeryClientAsync(cancellationToken: Cancellation);
        (await onClient.PostAsJsonAsync("/api/budgets", new CreateBudgetRequest(year), Cancellation)).EnsureSuccessStatusCode();
        await using PostgresTestWebApplicationFactory off = fixture.CreateFactory(new Dictionary<string, string?> { ["Features:Budget"] = "false" });
        using HttpClient offClient = await off.CreateAntiforgeryClientAsync(cancellationToken: Cancellation);

        HttpResponseMessage create = await offClient.PostAsJsonAsync("/api/budgets", new CreateBudgetRequest(UnusedYear()), Cancellation);
        HttpResponseMessage get = await offClient.GetAsync($"/api/budgets/{year}", Cancellation);

        ItShouldNotCreateABudget();
        ItShouldNotShowTheYearsBudget();

        void ItShouldNotCreateABudget() => create.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        void ItShouldNotShowTheYearsBudget() => get.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task SomeoneWhoIsNotSignedInCallsTheBudget()
    {
        await using TestWebApplicationFactory factory = new() { SimulateUnauthenticated = true, ConfigurationOverrides = BudgetOn };
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: Cancellation);

        HttpResponseMessage create = await client.PostAsJsonAsync("/api/budgets", new CreateBudgetRequest(2027), Cancellation);
        HttpResponseMessage get = await client.GetAsync("/api/budgets/2027", Cancellation);

        ItShouldTurnThemAway();

        void ItShouldTurnThemAway() => new[] { create.StatusCode, get.StatusCode }
            .ShouldAllBe(status => status == HttpStatusCode.Unauthorized);
    }

    private static async Task<JsonDocument> ReadProblemAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync(Cancellation));
}
