using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using System.Net;

namespace Menlo.Api.Tests.Ui;

/// <summary>
/// The parts of the HTML UI that need no database: sign-in, the redirects around it, the home page
/// shell, error pages, the design gallery and the anti-forgery check.
/// </summary>
public sealed class UiShellTests : TestFixture
{
    private static readonly WebApplicationFactoryClientOptions NoRedirects = new() { AllowAutoRedirect = false };

    private static CancellationToken Cancellation => TestContext.Current!.Execution.CancellationToken;

    [Test]
    public async Task GivenNobodySignedIn_WhenOpeningAPage()
    {
        await using TestWebApplicationFactory factory = new() { SimulateUnauthenticated = true };
        using HttpClient client = factory.CreateClient(NoRedirects);

        HttpResponseMessage response = await client.GetAsync("/analytics?period=year", Cancellation);

        ItShouldSendThemToSignInAndRememberWhereTheyWereGoing();

        void ItShouldSendThemToSignInAndRememberWhereTheyWereGoing()
        {
            response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
            response.Headers.Location!.OriginalString.ShouldBe("/sign-in?returnUrl=%2Fanalytics%3Fperiod%3Dyear");
        }
    }

    [Test]
    public async Task GivenNobodySignedIn_WhenArrivingFromAnotherSite()
    {
        await using TestWebApplicationFactory factory = new() { SimulateUnauthenticated = true };
        using HttpClient client = factory.CreateClient(NoRedirects);
        HttpRequestMessage request = new(HttpMethod.Get, "/analytics?q=a&b=\"c\"");
        request.Headers.Add("Sec-Fetch-Site", "cross-site");

        HttpResponseMessage response = await client.SendAsync(request, Cancellation);
        IHtmlDocument document = await response.DocumentAsync();

        ItShouldReloadTheSameAddressFromThisSiteSoTheSessionCookieIsSent();
        ItShouldNotBeCached();

        void ItShouldReloadTheSameAddressFromThisSiteSoTheSessionCookieIsSent()
        {
            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            IElement refresh = document.QuerySelector("meta[http-equiv='refresh']")!;
            refresh.GetAttribute("content").ShouldBe("0;url=/analytics?q=a&b=%22c%22");
            document.QuerySelector("a")!.GetAttribute("href").ShouldBe("/analytics?q=a&b=%22c%22");
        }

        void ItShouldNotBeCached() => response.Headers.CacheControl!.NoStore.ShouldBeTrue();
    }

    [Test]
    public async Task GivenNobodySignedIn_WhenHtmxAsksForAFragment()
    {
        await using TestWebApplicationFactory factory = new() { SimulateUnauthenticated = true };
        using HttpClient client = factory.CreateClient(NoRedirects);

        HttpResponseMessage response = await client.HtmxAsync(HttpMethod.Get, "/analytics");

        ItShouldAnswer401SoTheScriptCanSendTheBrowserToSignIn();

        void ItShouldAnswer401SoTheScriptCanSendTheBrowserToSignIn() =>
            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GivenNobodySignedIn_WhenOpeningTheSignInPage()
    {
        await using TestWebApplicationFactory factory = new() { SimulateUnauthenticated = true };
        using HttpClient client = factory.CreateClient(NoRedirects);

        HttpResponseMessage response = await client.GetAsync("/sign-in?returnUrl=%2Fanalytics", Cancellation);
        IHtmlDocument document = await response.DocumentAsync();

        ItShouldShowTheSignInButtonThatLeavesTheAppWithoutHtmx();
        ItShouldNotShowTheAppShellOrAnError();

        void ItShouldShowTheSignInButtonThatLeavesTheAppWithoutHtmx()
        {
            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            IElement button = document.ByTestId("sign-in-button");
            button.GetAttribute("href").ShouldBe("/auth/login?returnUrl=%2Fanalytics");
            button.GetAttribute("hx-boost").ShouldBe("false");
        }

        void ItShouldNotShowTheAppShellOrAnError()
        {
            document.FindByTestId("mnl-app-shell").ShouldBeNull();
            document.FindByTestId("sign-in-error").ShouldBeNull();
        }
    }

    [Test]
    [Arguments("https://evil.example/")]
    [Arguments("//evil.example/")]
    [Arguments("/\\evil.example/")]
    public async Task GivenAReturnUrlOnAnotherSite_WhenOpeningTheSignInPage_ThenItIsIgnored(string returnUrl)
    {
        await using TestWebApplicationFactory factory = new() { SimulateUnauthenticated = true };
        using HttpClient client = factory.CreateClient(NoRedirects);

        HttpResponseMessage response = await client.GetAsync($"/sign-in?returnUrl={Uri.EscapeDataString(returnUrl)}", Cancellation);
        IHtmlDocument document = await response.DocumentAsync();

        document.ByTestId("sign-in-button").GetAttribute("href").ShouldBe("/auth/login?returnUrl=%2F");
    }

    [Test]
    public async Task GivenAFailedSignIn_WhenOpeningTheSignInPage()
    {
        await using TestWebApplicationFactory factory = new() { SimulateUnauthenticated = true };
        using HttpClient client = factory.CreateClient(NoRedirects);

        HttpResponseMessage response = await client.GetAsync("/sign-in?error=auth_failed", Cancellation);
        IHtmlDocument document = await response.DocumentAsync();

        ItShouldSayThatSignInDidNotComplete();

        void ItShouldSayThatSignInDidNotComplete() =>
            document.ByTestId("sign-in-error").TextContent.ShouldContain("Sign-in didn't complete");
    }

    [Test]
    public async Task GivenSomeoneSignedIn_WhenOpeningTheSignInPage()
    {
        await using TestWebApplicationFactory factory = new();
        using HttpClient client = factory.CreateClient(NoRedirects);

        HttpResponseMessage response = await client.GetAsync("/sign-in?returnUrl=%2Fanalytics", Cancellation);

        ItShouldTakeThemWhereTheyWereGoing();

        void ItShouldTakeThemWhereTheyWereGoing()
        {
            response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
            response.Headers.Location!.OriginalString.ShouldBe("/analytics");
        }
    }

    [Test]
    public async Task GivenSomeoneSignedIn_WhenOpeningTheHomePage()
    {
        await using TestWebApplicationFactory factory = new() { UserDisplayName = "Sam Example" };
        using HttpClient client = factory.CreateClient(NoRedirects);

        HttpResponseMessage response = await client.GetAsync("/", Cancellation);
        IHtmlDocument document = await response.DocumentAsync();

        ItShouldRenderAFullHtmlPage();
        ItShouldShowTheNavigationWithHomeCurrent();
        ItShouldShowWhoIsSignedInAndASignOutButton();
        ItShouldLinkToAnalytics();

        void ItShouldRenderAFullHtmlPage()
        {
            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            response.Content.Headers.ContentType!.MediaType.ShouldBe("text/html");
            document.Title.ShouldBe("Home · Menlo");
        }

        void ItShouldShowTheNavigationWithHomeCurrent()
        {
            document.ByTestId("mnl-nav-item-/").GetAttribute("aria-current").ShouldBe("page");
            document.ByTestId("mnl-nav-item-/analytics").HasAttribute("aria-current").ShouldBeFalse();
        }

        void ItShouldShowWhoIsSignedInAndASignOutButton()
        {
            document.ByTestId("mnl-user-avatar").GetAttribute("aria-label").ShouldBe("Sam Example");
            IElement form = document.ByTestId("sign-out-button").Closest("form")!;
            form.GetAttribute("action").ShouldBe("/auth/logout");
            form.GetAttribute("method").ShouldBe("post");
            form.GetAttribute("hx-boost").ShouldBe("false");
        }

        void ItShouldLinkToAnalytics()
        {
            document.ByTestId("home-primary-action").GetAttribute("href").ShouldBe("/analytics");
            document.QuerySelectorAll("[data-testid='home-feature-card']").Length.ShouldBe(3);
        }
    }

    [Test]
    [Arguments("true", true)]
    [Arguments("false", false)]
    [Arguments(null, false)]
    public async Task GivenTheBudgetFlag_WhenOpeningAPage_ThenTheBudgetNavigationItemFollowsIt(string? flag, bool shown)
    {
        await using TestWebApplicationFactory factory = new()
        {
            // Not set at all when there is no flag
            ConfigurationOverrides = flag is null
                ? new Dictionary<string, string?>()
                : new Dictionary<string, string?> { ["Features:Budget"] = flag }
        };
        using HttpClient client = factory.CreateClient(NoRedirects);

        HttpResponseMessage response = await client.GetAsync("/", Cancellation);
        IHtmlDocument document = await response.DocumentAsync();

        ItShouldShowTheBudgetItemOnlyWhileTheFlagIsOn();
        ItShouldAlwaysShowHomeAndAnalytics();

        void ItShouldShowTheBudgetItemOnlyWhileTheFlagIsOn() =>
            (document.FindByTestId("mnl-nav-item-/budgets") is not null).ShouldBe(shown);

        void ItShouldAlwaysShowHomeAndAnalytics()
        {
            document.ByTestId("mnl-nav-item-/").ShouldNotBeNull();
            document.ByTestId("mnl-nav-item-/analytics").ShouldNotBeNull();
        }
    }

    [Test]
    public async Task GivenSomeoneWithoutABudgetRole_WhenOpeningAnalytics()
    {
        await using TestWebApplicationFactory factory = new() { UserRoles = [] };
        using HttpClient client = factory.CreateClient(NoRedirects);

        HttpResponseMessage page = await client.GetAsync("/analytics", Cancellation);
        IHtmlDocument document = await page.DocumentAsync();
        HttpResponseMessage fragment = await client.HtmxAsync(HttpMethod.Get, "/analytics");

        ItShouldShowTheNoAccessPage();
        ItShouldAnswerAFragmentRequestWithABare403();

        void ItShouldShowTheNoAccessPage()
        {
            page.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
            document.ByTestId("forbidden-page").TextContent.ShouldContain("You don't have access to this page");
        }

        void ItShouldAnswerAFragmentRequestWithABare403() => fragment.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task GivenAnAddressThatMatchesNothing_WhenRequested()
    {
        await using TestWebApplicationFactory factory = new();
        using HttpClient client = factory.CreateClient(NoRedirects);

        HttpResponseMessage page = await client.GetAsync("/projects/279/details", Cancellation);
        IHtmlDocument document = await page.DocumentAsync();
        HttpResponseMessage api = await client.GetAsync("/api/weatherforecast", Cancellation);
        HttpResponseMessage wellKnown = await client.GetAsync("/.well-known/security.txt", Cancellation);
        HttpResponseMessage file = await client.GetAsync("/missing.css", Cancellation);

        ItShouldShowTheNotFoundPageForAPage();
        ItShouldAnswerEverythingElseWithABare404();

        void ItShouldShowTheNotFoundPageForAPage()
        {
            page.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            document.ByTestId("not-found-page").ShouldNotBeNull();
        }

        void ItShouldAnswerEverythingElseWithABare404()
        {
            foreach (HttpResponseMessage response in new[] { api, wellKnown, file })
            {
                response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
                response.Content.Headers.ContentLength.ShouldBe(0);
            }
        }
    }

    [Test]
    public async Task GivenTheSignInCallback_WhenPostedWithoutAnAntiForgeryToken()
    {
        await using TestWebApplicationFactory factory = new() { SimulateUnauthenticated = true };
        using HttpClient client = factory.CreateClient(NoRedirects);

        HttpResponseMessage response = await client.PostAsync("/signin-oidc", content: null, Cancellation);

        // The identity provider posts here; the UI's catch-all route must leave it alone
        ItShouldNotBeRejectedByTheUi();

        void ItShouldNotBeRejectedByTheUi() => response.StatusCode.ShouldNotBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task GivenAUiAction_WhenPostedWithoutAnAntiForgeryToken()
    {
        await using TestWebApplicationFactory factory = new()
        {
            ConfigurationOverrides = new Dictionary<string, string?> { ["Features:DesignGallery"] = "true" }
        };
        using HttpClient client = factory.CreateClient(NoRedirects);

        HttpResponseMessage response = await client.HtmxAsync(HttpMethod.Post, "/design/toast?tone=error");

        ItShouldBeRefused();

        void ItShouldBeRefused() => response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task GivenProduction_WhenOpeningTheDesignGallery()
    {
        await using TestWebApplicationFactory factory = new() { SimulateUnauthenticated = true };
        using HttpClient client = factory.CreateClient(NoRedirects);

        HttpResponseMessage response = await client.GetAsync("/design", Cancellation);

        // Unknown pages ask for sign-in first, so a visitor can't tell which addresses exist
        ItShouldNotBeMapped();

        void ItShouldNotBeMapped() => response.Headers.Location!.OriginalString.ShouldStartWith("/sign-in");
    }

    [Test]
    public async Task GivenDevelopment_WhenOpeningTheDesignGalleryWithoutSigningIn()
    {
        await using TestWebApplicationFactory factory = new()
        {
            EnvironmentName = Environments.Development,
            SimulateUnauthenticated = true
        };
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(NoRedirects, Cancellation);

        HttpResponseMessage page = await client.GetAsync("/design", Cancellation);
        IHtmlDocument document = await page.DocumentAsync();
        HttpResponseMessage panel = await client.HtmxAsync(HttpMethod.Get, "/design/panel?mode=dialog");
        IHtmlDocument panelDocument = await panel.DocumentAsync();
        HttpResponseMessage toast = await client.HtmxAsync(HttpMethod.Post, "/design/toast?tone=error");

        ItShouldShowTheGallery();
        ItShouldServeTheExamplePanelAsAFragment();
        ItShouldAnswerTheToastButtonWithAnEventAndNoContent();

        void ItShouldShowTheGallery()
        {
            page.StatusCode.ShouldBe(HttpStatusCode.OK);
            document.ByTestId("gallery").ShouldNotBeNull();
            document.ByTestId("section-charts").ShouldNotBeNull();
        }

        void ItShouldServeTheExamplePanelAsAFragment()
        {
            panelDocument.QuerySelector("html head title").ShouldBeNull();
            panelDocument.ByTestId("example-panel").GetAttribute("data-mode").ShouldBe("dialog");
        }

        void ItShouldAnswerTheToastButtonWithAnEventAndNoContent()
        {
            toast.StatusCode.ShouldBe(HttpStatusCode.NoContent);
            toast.Triggers().ShouldContain("\"mnl:toast\"");
            toast.Triggers().ShouldContain("\"tone\":\"error\"");
        }
    }

    [Test]
    public async Task GivenTheGalleryIsSwitchedOn_WhenOpeningItInProduction()
    {
        await using TestWebApplicationFactory factory = new()
        {
            SimulateUnauthenticated = true,
            ConfigurationOverrides = new Dictionary<string, string?> { ["Features:DesignGallery"] = "true" }
        };
        using HttpClient client = factory.CreateClient(NoRedirects);

        HttpResponseMessage response = await client.GetAsync("/design", Cancellation);

        ItShouldBeServed();

        void ItShouldBeServed() => response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task GivenAPage_WhenItsStylesAndScriptsAreRequested()
    {
        await using TestWebApplicationFactory factory = new();
        using HttpClient client = factory.CreateClient(NoRedirects);
        IHtmlDocument document = await (await client.GetAsync("/", Cancellation)).DocumentAsync();

        List<string> assets =
        [
            .. document.QuerySelectorAll("link[rel='stylesheet']").Select(link => link.GetAttribute("href")!),
            .. document.QuerySelectorAll("script[src]").Select(script => script.GetAttribute("src")!)
        ];
        List<HttpStatusCode> statuses = [];
        foreach (string asset in assets)
        {
            statuses.Add((await client.GetAsync(asset, Cancellation)).StatusCode);
        }

        ItShouldFindEveryOneOnThisOrigin();

        void ItShouldFindEveryOneOnThisOrigin()
        {
            assets.Count.ShouldBeGreaterThanOrEqualTo(6);
            assets.ShouldAllBe(asset => asset.StartsWith('/'));
            statuses.ShouldAllBe(status => status == HttpStatusCode.OK);
        }
    }

    [Test]
    public async Task GivenAPeriod_WhenOpeningAnalytics()
    {
        await using TestWebApplicationFactory factory = new();
        using HttpClient client = factory.CreateClient(NoRedirects);

        IHtmlDocument quarter = await (await client.GetAsync("/analytics?period=quarter", Cancellation)).DocumentAsync();
        IHtmlDocument unknown = await (await client.GetAsync("/analytics?period=decade", Cancellation)).DocumentAsync();

        ItShouldShowTheChosenPeriod();
        ItShouldFallBackToTheCurrentMonthForAnUnknownPeriod();
        ItShouldShowTheSampleFigures();

        void ItShouldShowTheChosenPeriod()
        {
            quarter.ByTestId("budget-analytics-period-select").QuerySelector("option[selected]")!.GetAttribute("value").ShouldBe("quarter");
            quarter.ByTestId("budget-analytics-hero").ShouldNotBeNull();
            quarter.ByTestId("budget-analytics-category-1").TextContent.ShouldContain("budgeted in This Quarter");
        }

        void ItShouldFallBackToTheCurrentMonthForAnUnknownPeriod() =>
            unknown.ByTestId("budget-analytics-period-select").QuerySelector("option[selected]")!.GetAttribute("value").ShouldBe("current");

        void ItShouldShowTheSampleFigures()
        {
            quarter.QuerySelectorAll("[data-testid^='budget-analytics-stat-']").Length.ShouldBe(4);
            quarter.QuerySelectorAll("[data-testid^='budget-analytics-category-']").Length.ShouldBe(6);
            quarter.QuerySelectorAll("[data-testid^='budget-analytics-insight-']").Length.ShouldBe(3);
        }
    }
}
