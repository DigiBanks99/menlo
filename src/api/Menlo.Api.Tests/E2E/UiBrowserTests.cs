using Menlo.Api.Tests.Fixtures;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Menlo.Api.Tests.E2E;

/// <summary>
/// The UI in a real browser, against the real app and a real database: htmx swaps, panels, toasts,
/// the theme, and the Content Security Policy. Markup alone is covered by the faster tests in <c>Ui/</c>.
/// </summary>
[ClassDataSource<PostgresApiFixture, BrowserFixture>(Shared = [SharedType.PerTestSession, SharedType.PerTestSession])]
[Category("E2E")]
public sealed class UiBrowserTests(PostgresApiFixture fixture, BrowserFixture browser) : TestFixture
{
    [Test]
    public async Task GivenTheDesignGallery_WhenUsingItsLiveExamples()
    {
        PostgresTestWebApplicationFactory factory =
            fixture.CreateFactory(new Dictionary<string, string?> { ["Features:DesignGallery"] = "true" });
        await using AppSession app = await browser.StartAsync(factory, colorScheme: ColorScheme.Dark);
        IPage page = app.Page;
        ILocator html = page.Locator("html");

        await page.GotoAsync("/design");
        await ItShouldFollowTheSystemThemeWithCatppuccinMocha();

        await page.GetByTestId("gallery-theme-toggle").ClickAsync();
        await ItShouldSwitchToCatppuccinLatte();

        await page.ReloadAsync();
        await ItShouldRememberTheChoice();

        await page.GetByTestId("open-panel").ClickAsync();
        await Expect(page.GetByTestId("example-panel")).ToBeVisibleAsync();
        await page.Keyboard.PressAsync("Escape");
        await Expect(page.GetByTestId("example-panel")).ToHaveCountAsync(0);

        await page.GetByTestId("toast-error").ClickAsync();
        await ItShouldShowTheToastTheServerSent();

        await page.GetByTestId("rows-add").ClickAsync();
        await page.GetByTestId("rows").Locator("input[name='percent']").Last.FillAsync("40");
        await Expect(page.GetByTestId("rows-total")).ToHaveTextAsync("140%");

        await page.GetByTestId("disclosure").Locator("summary").First.ClickAsync();
        await Expect(page.GetByTestId("disclosure")).ToHaveAttributeAsync("open", string.Empty);

        await ItShouldDrawTheChartsInTheThemeColours();
        ItShouldNotHaveBrokenTheContentSecurityPolicy();

        async Task ItShouldFollowTheSystemThemeWithCatppuccinMocha()
        {
            await Expect(html).ToHaveAttributeAsync("data-theme", "dark");
            (await BackgroundAsync()).ShouldBe("rgb(30, 30, 46)");
        }

        async Task ItShouldSwitchToCatppuccinLatte()
        {
            await Expect(html).ToHaveAttributeAsync("data-theme", "light");
            (await BackgroundAsync()).ShouldBe("rgb(239, 241, 245)");
        }

        async Task ItShouldRememberTheChoice()
        {
            await Expect(html).ToHaveAttributeAsync("data-theme", "light");
            (await BackgroundAsync()).ShouldBe("rgb(239, 241, 245)");
        }

        async Task ItShouldShowTheToastTheServerSent()
        {
            ILocator toast = page.GetByTestId("mnl-toast").Last;
            await Expect(toast).ToHaveTextAsync("That didn't work. Nothing was changed.");
            await Expect(toast).ToHaveAttributeAsync("data-tone", "error");
        }

        async Task ItShouldDrawTheChartsInTheThemeColours()
        {
            // Latte pink, the first chart colour
            string fill = await page.GetByTestId("bar-chart").Locator("rect").First.EvaluateAsync<string>("bar => getComputedStyle(bar).fill");
            fill.ShouldBe("rgb(234, 118, 203)");
            (await page.GetByTestId("donut-chart").Locator("circle.mnl-chart__arc").CountAsync()).ShouldBe(5);
        }

        Task<string> BackgroundAsync() => page.EvaluateAsync<string>("() => getComputedStyle(document.body).backgroundColor");

        void ItShouldNotHaveBrokenTheContentSecurityPolicy() => app.BrowserErrors.ShouldBeEmpty(string.Join(" || ", app.BrowserErrors));
    }

    [Test]
    public async Task GivenAPhone_WhenOpeningTheApp()
    {
        await using AppSession app = await browser.StartAsync(
            fixture.CreateFactory(),
            viewport: new ViewportSize { Width = 390, Height = 844 });
        IPage page = app.Page;

        await page.GotoAsync("/");

        await ItShouldShowTheNavigationAsABarAtTheBottom();
        await ItShouldNotScrollSideways();

        async Task ItShouldShowTheNavigationAsABarAtTheBottom()
        {
            LocatorBoundingBoxResult? box = await page.GetByTestId("mnl-nav").BoundingBoxAsync();
            box.ShouldNotBeNull();
            box.Width.ShouldBe(390, tolerance: 1);
            (box.Y + box.Height).ShouldBe(844, tolerance: 1);
        }

        async Task ItShouldNotScrollSideways() =>
            (await page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= window.innerWidth")).ShouldBeTrue();
    }
}
