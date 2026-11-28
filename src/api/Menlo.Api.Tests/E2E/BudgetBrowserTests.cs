using Menlo.Api.Tests.Fixtures;
using Microsoft.Playwright;
using System.Text.RegularExpressions;
using static Microsoft.Playwright.Assertions;

namespace Menlo.Api.Tests.E2E;

/// <summary>
/// The budget page in a real browser, with the current month pinned: creating a year, and the side rail.
/// </summary>
[ClassDataSource<PostgresApiFixture, BrowserFixture>(Shared = [SharedType.PerTestSession, SharedType.PerTestSession])]
[Category("E2E")]
public sealed class BudgetBrowserTests(PostgresApiFixture fixture, BrowserFixture browser) : TestFixture
{
    private static readonly Dictionary<string, string?> BudgetOn = new() { ["Features:Budget"] = "true" };

    // The database is shared by the whole test session and holds one budget per year, so these tests take years no
    // other test uses
    private static int _lastYear = Random.Shared.Next(2100, 2900);

    private static int UnusedYear() => Interlocked.Increment(ref _lastYear);

    // 15 March of a year, at noon in South Africa
    private static DateTimeOffset MidMarch(int year) => new(year, 3, 15, 10, 0, 0, TimeSpan.Zero);

    [Test]
    public async Task AMemberCreatesTheCurrentYearsBudget()
    {
        int year = UnusedYear();
        await using AppSession app = await browser.StartAsync(fixture.CreateFactory(BudgetOn, MidMarch(year)));
        IPage page = app.Page;

        await page.GotoAsync("/budgets");
        await page.GetByTestId("budget-create").ClickAsync();
        await ItShouldShowTheNewYearsBudget();
        await ItShouldSayItIsReady();
        await ItShouldMarkMarchAsTheCurrentMonth();

        await page.GetByTestId("budget-rail-proposals").ClickAsync();
        await ItShouldOpenTheSectionAndKeepItInTheAddress();

        await page.GetByTestId("budget-rail-close").ClickAsync();
        await ItShouldCloseTheSection();

        ItShouldNotHaveBrokenTheContentSecurityPolicy();

        async Task ItShouldShowTheNewYearsBudget()
        {
            await Expect(page).ToHaveURLAsync(new Regex($"/budgets\\?year={year}$"));
            await Expect(page.GetByTestId("budget-title")).ToHaveTextAsync($"{year} budget");
            await Expect(page.GetByTestId("budget-grid")).ToBeVisibleAsync();
        }

        async Task ItShouldSayItIsReady() =>
            await Expect(page.GetByTestId("mnl-toast").Last).ToHaveTextAsync($"The {year} budget is ready.");

        async Task ItShouldMarkMarchAsTheCurrentMonth()
        {
            ILocator months = page.GetByTestId("budget-month");
            await Expect(months.Nth(2)).ToHaveAttributeAsync("aria-current", "date");
            await Expect(months.Nth(1)).ToHaveAttributeAsync("data-month", "frozen");
            string march = await months.Nth(2).EvaluateAsync<string>("tile => getComputedStyle(tile).backgroundColor");
            string april = await months.Nth(3).EvaluateAsync<string>("tile => getComputedStyle(tile).backgroundColor");
            march.ShouldNotBe(april);
        }

        async Task ItShouldOpenTheSectionAndKeepItInTheAddress()
        {
            await Expect(page.GetByTestId("budget-rail-panel")).ToContainTextAsync("No proposals yet");
            await Expect(page.GetByTestId("budget-rail-proposals")).ToHaveAttributeAsync("aria-expanded", "true");
            await Expect(page).ToHaveURLAsync(new Regex($"/budgets\\?year={year}&panel=proposals$"));
        }

        async Task ItShouldCloseTheSection()
        {
            await Expect(page.GetByTestId("budget-rail-panel")).ToHaveCountAsync(0);
            await Expect(page).ToHaveURLAsync(new Regex($"/budgets\\?year={year}$"));
        }

        void ItShouldNotHaveBrokenTheContentSecurityPolicy() => app.BrowserErrors.ShouldBeEmpty(string.Join(" || ", app.BrowserErrors));
    }

    [Test]
    public async Task AMemberOpensAYearsBudgetOnAPhone()
    {
        int year = UnusedYear();
        await using AppSession app = await browser.StartAsync(
            fixture.CreateFactory(BudgetOn, MidMarch(year)),
            viewport: new ViewportSize { Width = 390, Height = 844 },
            colorScheme: ColorScheme.Dark);
        IPage page = app.Page;

        await page.GotoAsync("/budgets");
        await page.GetByTestId("budget-create").ClickAsync();
        await Expect(page.GetByTestId("budget-grid")).ToBeVisibleAsync();

        await ItShouldNotScrollSideways();
        ItShouldNotHaveBrokenTheContentSecurityPolicy();

        async Task ItShouldNotScrollSideways() =>
            (await page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= window.innerWidth")).ShouldBeTrue();

        void ItShouldNotHaveBrokenTheContentSecurityPolicy() => app.BrowserErrors.ShouldBeEmpty(string.Join(" || ", app.BrowserErrors));
    }
}
