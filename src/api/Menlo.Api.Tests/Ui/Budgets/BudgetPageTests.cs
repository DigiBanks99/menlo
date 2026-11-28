using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Menlo.Api.Budget;
using Menlo.Api.Tests.Fixtures;
using Menlo.Ui.Components;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Menlo.Api.Tests.Ui.Budgets;

/// <summary>
/// The budget page at <c>/budgets</c>, against a real PostgreSQL database, with the current month pinned.
/// </summary>
[ClassDataSource<PostgresApiFixture>(Shared = SharedType.PerTestSession)]
public sealed class BudgetPageTests(PostgresApiFixture fixture) : TestFixture
{
    private static readonly Dictionary<string, string?> BudgetOn = new() { ["Features:Budget"] = "true" };
    private static readonly WebApplicationFactoryClientOptions NoRedirects = new() { AllowAutoRedirect = false };

    // The database is shared by the whole test session and holds one budget per year, so each test takes years no
    // other test uses
    private static int _lastYear = Random.Shared.Next(6000, 9000);

    private static CancellationToken Cancellation => TestContext.Current!.Execution.CancellationToken;

    private static int UnusedYear() => Interlocked.Increment(ref _lastYear);

    // 15 July of a year, at noon in South Africa
    private static DateTimeOffset MidJuly(int year) => new(year, 7, 15, 10, 0, 0, TimeSpan.Zero);

    [Test]
    public async Task AMemberOpensTheBudgetBeforeTheCurrentYearHasOne()
    {
        int year = UnusedYear();
        await using PostgresTestWebApplicationFactory factory = fixture.CreateFactory(BudgetOn, MidJuly(year));
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/budgets", Cancellation);
        IHtmlDocument document = await response.DocumentAsync();

        ItShouldShowTheCurrentYear();
        ItShouldOfferToCreateItsBudget();
        ItShouldNotShowTheFrameOfABudget();

        void ItShouldShowTheCurrentYear() => document.ShouldSatisfyAllConditions(
            () => response.StatusCode.ShouldBe(HttpStatusCode.OK),
            () => document.ByTestId("budget-title").TextContent.Trim().ShouldBe($"{year} budget"),
            () => document.Title.ShouldBe($"{year} budget · Menlo"));

        void ItShouldOfferToCreateItsBudget()
        {
            IElement offer = document.ByTestId("budget-create-offer");
            IElement form = offer.QuerySelector("form")!;
            form.GetAttribute("hx-post").ShouldBe("/budgets");
            form.QuerySelector("input[name='year']")!.GetAttribute("value").ShouldBe(year.ToString());
            offer.ByTestId("budget-create").TextContent.Trim().ShouldBe($"Create the {year} budget");
        }

        void ItShouldNotShowTheFrameOfABudget() => new[] { "budget-month-strip", "budget-frame", "budget-grid" }
            .ShouldAllBe(id => document.FindByTestId(id) == null);
    }

    [Test]
    public async Task AMemberCreatesTheCurrentYearsBudget()
    {
        int year = UnusedYear();
        await using PostgresTestWebApplicationFactory factory = fixture.CreateFactory(BudgetOn, MidJuly(year));
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(NoRedirects, Cancellation);

        HttpResponseMessage created = await client.HtmxAsync(HttpMethod.Post, "/budgets", [new("year", year.ToString())]);
        HttpResponseMessage stored = await client.GetAsync($"/api/budgets/{year}", Cancellation);

        ItShouldGoToTheYearsBudget();
        ItShouldSayItIsReady();
        ItShouldStoreTheBudget();

        void ItShouldGoToTheYearsBudget() => created.ShouldSatisfyAllConditions(
            () => created.StatusCode.ShouldBe(HttpStatusCode.NoContent),
            () => created.Headers.GetValues("HX-Location").Single().ShouldContain($"/budgets?year={year}"));

        void ItShouldSayItIsReady() => created.Headers.GetValues("Set-Cookie").ShouldContain(c => c.StartsWith(".Menlo.Flash=", StringComparison.Ordinal));

        void ItShouldStoreTheBudget() => stored.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task AMemberCreatesABudgetSomeoneElseJustCreated()
    {
        int year = UnusedYear();
        await using PostgresTestWebApplicationFactory factory = fixture.CreateFactory(BudgetOn, MidJuly(year));
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(NoRedirects, Cancellation);
        (await client.PostAsJsonAsync("/api/budgets", new CreateBudgetRequest(year), Cancellation)).EnsureSuccessStatusCode();

        HttpResponseMessage response = await client.HtmxAsync(HttpMethod.Post, "/budgets", [new("year", year.ToString())]);

        ItShouldShowThemTheYearsBudget();

        void ItShouldShowThemTheYearsBudget() => response.ShouldSatisfyAllConditions(
            () => response.StatusCode.ShouldBe(HttpStatusCode.NoContent),
            () => response.Headers.GetValues("HX-Location").Single().ShouldContain($"/budgets?year={year}"));
    }

    [Test]
    [Arguments("0", "0 isn't a budget year.")]
    [Arguments("", "Choose a year for the budget.")]
    [Arguments("next", "Choose a year for the budget.")]
    public async Task AMemberCreatesABudgetForSomethingThatIsntABudgetYear(string year, string message)
    {
        await using PostgresTestWebApplicationFactory factory = fixture.CreateFactory(BudgetOn);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(NoRedirects, Cancellation);

        HttpResponseMessage response = await client.HtmxAsync(HttpMethod.Post, "/budgets", [new("year", year)]);
        using JsonDocument triggers = JsonDocument.Parse(response.Triggers());
        JsonElement toast = triggers.RootElement.GetProperty(MnlToastMessage.EventName);

        ItShouldStayOnThePage();
        ItShouldSayWhy();

        void ItShouldStayOnThePage() => response.ShouldSatisfyAllConditions(
            () => response.StatusCode.ShouldBe(HttpStatusCode.NoContent),
            () => response.Headers.Contains("HX-Location").ShouldBeFalse());

        void ItShouldSayWhy() => toast.ShouldSatisfyAllConditions(
            t => t.GetProperty("message").GetString().ShouldBe(message),
            t => t.GetProperty("tone").GetString().ShouldBe("error"));
    }

    [Test]
    public async Task AMemberOpensTheCurrentYearsBudgetInJuly()
    {
        int year = UnusedYear();
        int other = UnusedYear();
        await using PostgresTestWebApplicationFactory factory = fixture.CreateFactory(BudgetOn, MidJuly(year));
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: Cancellation);
        (await client.PostAsJsonAsync("/api/budgets", new CreateBudgetRequest(year), Cancellation)).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/api/budgets", new CreateBudgetRequest(other), Cancellation)).EnsureSuccessStatusCode();

        HttpResponseMessage response = await client.GetAsync("/budgets", Cancellation);
        IHtmlDocument document = await response.DocumentAsync();
        IElement[] months = [.. document.QuerySelectorAll("[data-testid='budget-month']")];
        IElement[] columns = [.. document.QuerySelectorAll("[data-testid='budget-grid-month']")];

        ItShouldShowTheYearsBudget();
        ItShouldShadeJanuaryToJuneAsFrozen();
        ItShouldMarkJulyAsTheCurrentMonth();
        ItShouldEndTheStripWithTheYear();
        ItShouldMarkTheGridsMonthsTheSameWay();
        ItShouldShowAnEmptyGrid();
        ItShouldShowTheSideRailsIcons();
        ItShouldLetThemPickAnotherYear();

        void ItShouldShowTheYearsBudget() => document.ShouldSatisfyAllConditions(
            () => response.StatusCode.ShouldBe(HttpStatusCode.OK),
            () => document.ByTestId("budget-title").TextContent.Trim().ShouldBe($"{year} budget"),
            () => document.FindByTestId("budget-create-offer").ShouldBeNull());

        void ItShouldShadeJanuaryToJuneAsFrozen() => months.Take(6).ShouldAllBe(m =>
            m.GetAttribute("data-month") == "frozen" && m.TextContent.Contains("Frozen"));

        void ItShouldMarkJulyAsTheCurrentMonth()
        {
            months.Length.ShouldBe(12);
            months[6].GetAttribute("data-month").ShouldBe("current");
            months[6].GetAttribute("aria-current").ShouldBe("date");
            months[6].TextContent.ShouldContain("This month");
            months.Where(m => m.HasAttribute("aria-current")).ShouldHaveSingleItem();
            months.Skip(7).ShouldAllBe(m => m.GetAttribute("data-month") == "ahead");
        }

        void ItShouldEndTheStripWithTheYear() => document.ByTestId("budget-month-year").TextContent.Trim().ShouldBe("Year");

        void ItShouldMarkTheGridsMonthsTheSameWay() => columns
            .Select(c => c.GetAttribute("data-month"))
            .ShouldBe(months.Select(m => m.GetAttribute("data-month")));

        void ItShouldShowAnEmptyGrid() => document.ByTestId("budget-grid").ShouldSatisfyAllConditions(
            g => g.QuerySelector("th")!.TextContent.Trim().ShouldBe("Category"),
            g => g.QuerySelectorAll("tbody tr").ShouldBeEmpty(),
            g => g.ByTestId("budget-grid-empty").TextContent.ShouldContain("No categories yet"));

        void ItShouldShowTheSideRailsIcons()
        {
            IElement rail = document.ByTestId("budget-rail");
            rail.QuerySelectorAll("a").Select(a => a.GetAttribute("title")).ShouldBe(
                ["Proposals", "Triggers", "Set-up checklist", "Year tools", "Who it is for", "How to read the grid"]);
            rail.QuerySelectorAll("a").ShouldAllBe(a => a.GetAttribute("aria-expanded") == "false");
            document.FindByTestId("budget-rail-panel").ShouldBeNull();
        }

        void ItShouldLetThemPickAnotherYear()
        {
            IHtmlSelectElement picker = (IHtmlSelectElement)document.ByTestId("budget-year-picker");
            picker.Value.ShouldBe(year.ToString());
            picker.Options.Select(o => o.Value).ShouldContain(other.ToString());
        }
    }

    [Test]
    public async Task AMemberOpensASectionOfTheSideRail()
    {
        int year = UnusedYear();
        await using PostgresTestWebApplicationFactory factory = fixture.CreateFactory(BudgetOn, MidJuly(year));
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: Cancellation);
        (await client.PostAsJsonAsync("/api/budgets", new CreateBudgetRequest(year), Cancellation)).EnsureSuccessStatusCode();

        HttpResponseMessage response = await client.GetAsync($"/budgets?year={year}&panel=proposals", Cancellation);
        IHtmlDocument document = await response.DocumentAsync();
        IElement icon = document.ByTestId("budget-rail-proposals");

        ItShouldShowTheSectionEmpty();
        ItShouldMarkItsIconAsOpen();
        ItShouldSwapOnlyTheFrameWhenTheIconIsClicked();

        void ItShouldShowTheSectionEmpty() => document.ByTestId("budget-rail-panel").ShouldSatisfyAllConditions(
            p => p.QuerySelector("h2")!.TextContent.Trim().ShouldBe("Proposals"),
            p => p.TextContent.ShouldContain("No proposals yet"));

        void ItShouldMarkItsIconAsOpen() => icon.ShouldSatisfyAllConditions(
            i => i.GetAttribute("aria-expanded").ShouldBe("true"),
            i => i.GetAttribute("aria-controls").ShouldBe("budget-rail-panel"),
            i => i.GetAttribute("href").ShouldBe($"/budgets?year={year}"));

        void ItShouldSwapOnlyTheFrameWhenTheIconIsClicked() => icon.ShouldSatisfyAllConditions(
            i => i.GetAttribute("hx-get").ShouldBe($"/budgets?year={year}"),
            i => i.GetAttribute("hx-target").ShouldBe("#budget-frame"),
            i => i.GetAttribute("hx-select").ShouldBe("#budget-frame"),
            i => i.GetAttribute("hx-sync").ShouldBe("#budget-frame:replace"));
    }

    [Test]
    public async Task TheBudgetIsSwitchedOff()
    {
        await using PostgresTestWebApplicationFactory factory = fixture.CreateFactory(new Dictionary<string, string?> { ["Features:Budget"] = "false" });
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(NoRedirects, Cancellation);

        HttpResponseMessage page = await client.GetAsync("/budgets", Cancellation);
        IHtmlDocument document = await page.DocumentAsync();
        HttpResponseMessage create = await client.HtmxAsync(HttpMethod.Post, "/budgets", [new("year", UnusedYear().ToString())]);

        ItShouldShowTheNotFoundPage();
        ItShouldNotCreateABudget();

        void ItShouldShowTheNotFoundPage() => document.ShouldSatisfyAllConditions(
            () => page.StatusCode.ShouldBe(HttpStatusCode.NotFound),
            () => document.FindByTestId("budget-page").ShouldBeNull());

        void ItShouldNotCreateABudget() => create.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
