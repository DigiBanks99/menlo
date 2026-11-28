using Menlo.Api.Budget;
using Menlo.Api.Tests.Fixtures;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Menlo.Api.Tests.Budget;

/// <summary>
/// <c>POST</c>, <c>GET</c> and <c>PATCH /api/rental-units</c>, against a real PostgreSQL database.
/// </summary>
[ClassDataSource<PostgresApiFixture>(Shared = SharedType.PerTestSession)]
public sealed class RentalUnitEndpointTests(PostgresApiFixture fixture) : TestFixture
{
    private static readonly Dictionary<string, string?> BudgetOn = new() { ["Features:Budget"] = "true" };
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private static CancellationToken Cancellation => TestContext.Current!.Execution.CancellationToken;

    // The database is shared by the whole test session, so each test names its units so it can tell them apart
    private static string UniqueName(string name) => $"{name} {Guid.NewGuid():N}";

    [Test]
    public async Task AMemberAddsARentalUnit()
    {
        string name = UniqueName("Garden flat");
        await using PostgresTestWebApplicationFactory factory = fixture.CreateFactory(BudgetOn);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: Cancellation);

        HttpResponseMessage created = await client.PostAsJsonAsync("/api/rental-units", new CreateRentalUnitRequest($"  {name} "), Cancellation);
        RentalUnitDto? unit = await created.Content.ReadFromJsonAsync<RentalUnitDto>(Json, Cancellation);
        HttpResponseMessage fetched = await client.GetAsync($"/api/rental-units/{unit?.Id}", Cancellation);
        RentalUnitDto? stored = await fetched.Content.ReadFromJsonAsync<RentalUnitDto>(Json, Cancellation);

        ItShouldBeCreated();
        ItShouldPointToTheUnit();
        ItShouldHaveItsNameWithoutTheSpaces();
        ItShouldBeTheUnitFetchedByItsId();

        void ItShouldBeCreated() => created.StatusCode.ShouldBe(HttpStatusCode.Created);

        void ItShouldPointToTheUnit() => created.Headers.Location.ShouldBe(new Uri($"/api/rental-units/{unit!.Id}", UriKind.Relative));

        void ItShouldHaveItsNameWithoutTheSpaces() => unit.ShouldNotBeNull().ShouldSatisfyAllConditions(
            u => u.Name.ShouldBe(name),
            u => u.Id.ShouldNotBe(Guid.Empty));

        void ItShouldBeTheUnitFetchedByItsId() => fetched.ShouldSatisfyAllConditions(
            () => fetched.StatusCode.ShouldBe(HttpStatusCode.OK),
            () => stored.ShouldBe(unit));
    }

    [Test]
    [Arguments("")]
    [Arguments("   ")]
    public async Task AMemberAddsARentalUnitWithoutAName(string name)
    {
        await using PostgresTestWebApplicationFactory factory = fixture.CreateFactory(BudgetOn);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: Cancellation);

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/rental-units", new CreateRentalUnitRequest(name), Cancellation);
        using JsonDocument problem = await ReadProblemAsync(response);

        ItShouldBeRefused();
        ItShouldSayWhy();

        void ItShouldBeRefused() => response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        void ItShouldSayWhy() => problem.RootElement.ShouldSatisfyAllConditions(
            p => p.GetProperty("code").GetString().ShouldBe("RentalUnit.InvalidName"),
            p => p.GetProperty("detail").GetString().ShouldBe("A rental unit needs a name of 1 to 100 characters."));
    }

    [Test]
    public async Task AMemberAddsARentalUnitWithAnEmptyBody()
    {
        await using PostgresTestWebApplicationFactory factory = fixture.CreateFactory(BudgetOn);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: Cancellation);

        HttpResponseMessage response = await client.PostAsync("/api/rental-units", JsonBody("{}"), Cancellation);
        using JsonDocument problem = await ReadProblemAsync(response);

        ItShouldBeRefusedForTheMissingName();

        void ItShouldBeRefusedForTheMissingName() => response.ShouldSatisfyAllConditions(
            () => response.StatusCode.ShouldBe(HttpStatusCode.BadRequest),
            () => problem.RootElement.GetProperty("code").GetString().ShouldBe("RentalUnit.InvalidName"));
    }

    [Test]
    public async Task AMemberAddsARentalUnitWithANameThatIsTooLong()
    {
        await using PostgresTestWebApplicationFactory factory = fixture.CreateFactory(BudgetOn);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: Cancellation);

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/rental-units", new CreateRentalUnitRequest(new string('a', 101)), Cancellation);

        ItShouldBeRefused();

        void ItShouldBeRefused() => response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task AMemberListsTheRentalUnits()
    {
        string cottage = UniqueName("Cottage");
        string flat = UniqueName("Flat");
        await using PostgresTestWebApplicationFactory factory = fixture.CreateFactory(BudgetOn);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: Cancellation);
        RentalUnitDto addedFlat = await AddAsync(client, flat);
        RentalUnitDto addedCottage = await AddAsync(client, cottage);

        HttpResponseMessage response = await client.GetAsync("/api/rental-units", Cancellation);
        List<RentalUnitDto>? units = await response.Content.ReadFromJsonAsync<List<RentalUnitDto>>(Json, Cancellation);

        ItShouldListThem();
        ItShouldListThemByName();

        void ItShouldListThem() => response.ShouldSatisfyAllConditions(
            () => response.StatusCode.ShouldBe(HttpStatusCode.OK),
            () => units.ShouldNotBeNull().ShouldContain(addedFlat),
            () => units.ShouldNotBeNull().ShouldContain(addedCottage));

        void ItShouldListThemByName() => units!.IndexOf(addedCottage).ShouldBeLessThan(units.IndexOf(addedFlat));
    }

    [Test]
    public async Task AMemberRenamesARentalUnit()
    {
        string newName = UniqueName("Cottage");
        await using PostgresTestWebApplicationFactory factory = fixture.CreateFactory(BudgetOn);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: Cancellation);
        RentalUnitDto added = await AddAsync(client, UniqueName("Garden flat"));

        HttpResponseMessage response = await client.PatchAsJsonAsync($"/api/rental-units/{added.Id}", new UpdateRentalUnitRequest(newName), Cancellation);
        RentalUnitDto? renamed = await response.Content.ReadFromJsonAsync<RentalUnitDto>(Json, Cancellation);
        RentalUnitDto? stored = await client.GetFromJsonAsync<RentalUnitDto>($"/api/rental-units/{added.Id}", Json, Cancellation);

        ItShouldBeRenamed();
        ItShouldKeepItsIdentity();
        ItShouldStoreTheNewName();

        void ItShouldBeRenamed() => response.ShouldSatisfyAllConditions(
            () => response.StatusCode.ShouldBe(HttpStatusCode.OK),
            () => renamed.ShouldNotBeNull().Name.ShouldBe(newName));

        void ItShouldKeepItsIdentity() => renamed!.Id.ShouldBe(added.Id);

        void ItShouldStoreTheNewName() => stored.ShouldBe(renamed);
    }

    [Test]
    public async Task AMemberChangesARentalUnitWithoutAName()
    {
        await using PostgresTestWebApplicationFactory factory = fixture.CreateFactory(BudgetOn);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: Cancellation);
        RentalUnitDto added = await AddAsync(client, UniqueName("Garden flat"));

        HttpResponseMessage response = await client.PatchAsync($"/api/rental-units/{added.Id}", JsonBody("{}"), Cancellation);
        RentalUnitDto? unit = await response.Content.ReadFromJsonAsync<RentalUnitDto>(Json, Cancellation);

        ItShouldKeepTheName();

        void ItShouldKeepTheName() => response.ShouldSatisfyAllConditions(
            () => response.StatusCode.ShouldBe(HttpStatusCode.OK),
            () => unit.ShouldBe(added));
    }

    [Test]
    public async Task AMemberRenamesARentalUnitToABlankName()
    {
        await using PostgresTestWebApplicationFactory factory = fixture.CreateFactory(BudgetOn);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: Cancellation);
        RentalUnitDto added = await AddAsync(client, UniqueName("Garden flat"));

        HttpResponseMessage response = await client.PatchAsJsonAsync($"/api/rental-units/{added.Id}", new UpdateRentalUnitRequest(" "), Cancellation);
        using JsonDocument problem = await ReadProblemAsync(response);
        RentalUnitDto? stored = await client.GetFromJsonAsync<RentalUnitDto>($"/api/rental-units/{added.Id}", Json, Cancellation);

        ItShouldBeRefused();
        ItShouldKeepTheName();

        void ItShouldBeRefused() => response.ShouldSatisfyAllConditions(
            () => response.StatusCode.ShouldBe(HttpStatusCode.BadRequest),
            () => problem.RootElement.GetProperty("code").GetString().ShouldBe("RentalUnit.InvalidName"));

        void ItShouldKeepTheName() => stored.ShouldBe(added);
    }

    [Test]
    public async Task AMemberOpensARentalUnitThatDoesNotExist()
    {
        Guid missing = Guid.NewGuid();
        await using PostgresTestWebApplicationFactory factory = fixture.CreateFactory(BudgetOn);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: Cancellation);

        HttpResponseMessage get = await client.GetAsync($"/api/rental-units/{missing}", Cancellation);
        HttpResponseMessage patch = await client.PatchAsJsonAsync($"/api/rental-units/{missing}", new UpdateRentalUnitRequest("Cottage"), Cancellation);

        ItShouldFindNoUnit();

        void ItShouldFindNoUnit() => new[] { get.StatusCode, patch.StatusCode }
            .ShouldAllBe(status => status == HttpStatusCode.NotFound);
    }

    [Test]
    public async Task TheBudgetIsSwitchedOff()
    {
        await using PostgresTestWebApplicationFactory on = fixture.CreateFactory(BudgetOn);
        using HttpClient onClient = await on.CreateAntiforgeryClientAsync(cancellationToken: Cancellation);
        RentalUnitDto added = await AddAsync(onClient, UniqueName("Garden flat"));
        await using PostgresTestWebApplicationFactory off = fixture.CreateFactory(new Dictionary<string, string?> { ["Features:Budget"] = "false" });
        using HttpClient offClient = await off.CreateAntiforgeryClientAsync(cancellationToken: Cancellation);

        HttpResponseMessage create = await offClient.PostAsJsonAsync("/api/rental-units", new CreateRentalUnitRequest("Cottage"), Cancellation);
        HttpResponseMessage list = await offClient.GetAsync("/api/rental-units", Cancellation);
        HttpResponseMessage get = await offClient.GetAsync($"/api/rental-units/{added.Id}", Cancellation);
        HttpResponseMessage patch = await offClient.PatchAsJsonAsync($"/api/rental-units/{added.Id}", new UpdateRentalUnitRequest("Cottage"), Cancellation);
        RentalUnitDto? stored = await onClient.GetFromJsonAsync<RentalUnitDto>($"/api/rental-units/{added.Id}", Json, Cancellation);

        ItShouldAnswerNotFoundEverywhere();
        ItShouldChangeNothing();

        void ItShouldAnswerNotFoundEverywhere() => new[] { create.StatusCode, list.StatusCode, get.StatusCode, patch.StatusCode }
            .ShouldAllBe(status => status == HttpStatusCode.NotFound);

        void ItShouldChangeNothing() => stored.ShouldBe(added);
    }

    [Test]
    public async Task SomeoneWhoIsNotSignedInCallsTheRentalUnits()
    {
        await using TestWebApplicationFactory factory = new() { SimulateUnauthenticated = true, ConfigurationOverrides = BudgetOn };
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: Cancellation);
        Guid id = Guid.NewGuid();

        HttpResponseMessage create = await client.PostAsJsonAsync("/api/rental-units", new CreateRentalUnitRequest("Cottage"), Cancellation);
        HttpResponseMessage list = await client.GetAsync("/api/rental-units", Cancellation);
        HttpResponseMessage get = await client.GetAsync($"/api/rental-units/{id}", Cancellation);
        HttpResponseMessage patch = await client.PatchAsJsonAsync($"/api/rental-units/{id}", new UpdateRentalUnitRequest("Cottage"), Cancellation);

        ItShouldTurnThemAway();

        void ItShouldTurnThemAway() => new[] { create.StatusCode, list.StatusCode, get.StatusCode, patch.StatusCode }
            .ShouldAllBe(status => status == HttpStatusCode.Unauthorized);
    }

    private static async Task<RentalUnitDto> AddAsync(HttpClient client, string name)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/rental-units", new CreateRentalUnitRequest(name), Cancellation);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<RentalUnitDto>(Json, Cancellation))!;
    }

    private static StringContent JsonBody(string json) => new(json, Encoding.UTF8, "application/json");

    private static async Task<JsonDocument> ReadProblemAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync(Cancellation));
}
