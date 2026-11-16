using Menlo.Api.Tests.Budget;
using Menlo.Api.Tests.Ui;
using Microsoft.Extensions.Hosting;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Menlo.Api.Tests.E2E;

/// <summary>
/// The UI in a real browser, against the real app and a real database: htmx swaps, panels, toasts,
/// the theme, and the Content Security Policy. Markup alone is covered by the faster tests in <c>Ui/</c>.
/// </summary>
[ClassDataSource<BudgetApiFixture, BrowserFixture>(Shared = [SharedType.PerTestSession, SharedType.PerTestSession])]
[NotInParallel("Budget")]
[Category("E2E")]
public sealed class UiBrowserTests(BudgetApiFixture fixture, BrowserFixture browser) : TestFixture
{
    private static CancellationToken Cancellation => TestContext.Current!.Execution.CancellationToken;

    private static int CurrentYear => DateTimeOffset.Now.Year;

    [Test]
    public async Task GivenNoBudgets_WhenCreatingOneFromTheHomePageAndTheList()
    {
        await using AppSession app = await browser.StartAsync(fixture.CreateIsolatedFactory(budgetItemsEnabled: true));
        IPage page = app.Page;

        await page.GotoAsync("/");
        await ItShouldLoadTheBudgetWidgetAfterThePage();

        await page.GetByTestId("mnl-nav-item-/budgets").ClickAsync();
        await ItShouldNavigateWithoutReloadingTheDocument();

        await page.GetByTestId("create-budget-button").ClickAsync();
        await ItShouldLandOnTheNewBudgetWithAToast();

        ItShouldNotHaveUpsetTheBrowser();

        async Task ItShouldLoadTheBudgetWidgetAfterThePage()
        {
            await Expect(page.GetByTestId("widget-status")).ToHaveTextAsync("Draft");
            await Expect(page.GetByTestId("view-budget-button")).ToHaveAttributeAsync("href", new System.Text.RegularExpressions.Regex("^/budgets/"));
            await page.EvaluateAsync("() => { window.__sameDocument = true; }");
        }

        async Task ItShouldNavigateWithoutReloadingTheDocument()
        {
            await Expect(page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/budgets$"));
            await Expect(page).ToHaveTitleAsync("Budgets · Menlo");
            (await page.EvaluateAsync<bool>("() => window.__sameDocument === true")).ShouldBeTrue();
            await Expect(page.GetByTestId("mnl-nav-item-/budgets")).ToHaveAttributeAsync("aria-current", "page");
            await Expect(page.GetByTestId("create-budget-button")).ToHaveTextAsync($"Create {CurrentYear + 1} Budget");
        }

        async Task ItShouldLandOnTheNewBudgetWithAToast()
        {
            await Expect(page.GetByTestId("budget-year")).ToHaveTextAsync($"{CurrentYear + 1} Budget");
            await Expect(page.GetByTestId("mnl-toast")).ToHaveTextAsync($"Budget {CurrentYear + 1} created.");
            await Expect(page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/budgets/[0-9a-f-]{36}$"));
        }

        void ItShouldNotHaveUpsetTheBrowser() => app.BrowserErrors.ShouldBeEmpty(string.Join(" || ", app.BrowserErrors));
    }

    [Test]
    public async Task GivenABudget_WhenManagingItsCategories()
    {
        await using BudgetTestWebApplicationFactory api = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await api.CreateAntiforgeryClientAsync(cancellationToken: Cancellation);
        Guid budgetId = await UiHtml.CreateBudgetAsync(client, CurrentYear);
        Guid work = await UiHtml.CreateCategoryAsync(client, budgetId, "Work", "Income");

        await using AppSession app = await browser.StartAsync(fixture.CreateIsolatedFactory(budgetItemsEnabled: true, keepData: true));
        IPage page = app.Page;
        ILocator panel = page.GetByTestId("category-form-panel");
        ILocator tree = page.GetByTestId("tree-list");

        await page.GotoAsync($"/budgets/{budgetId}");

        await page.GetByTestId("btn-add-root").ClickAsync();
        await panel.GetByTestId("btn-save").ClickAsync();
        await ItShouldKeepThePanelOpenAndShowWhatIsMissing();

        await panel.GetByTestId("input-name").FillAsync("Housing");
        await panel.GetByTestId("select-budgetFlow").SelectOptionAsync("Expense");
        await panel.GetByTestId("btn-save").ClickAsync();
        await ItShouldCloseThePanelConfirmAndShowTheCategory("Category created.", "Housing");

        await tree.Locator("li", new() { HasText = "Housing" }).GetByText("Add child").ClickAsync();
        await panel.GetByTestId("input-name").FillAsync("Rent");
        await panel.GetByTestId("select-budgetFlow").SelectOptionAsync("Expense");
        await panel.GetByTestId("btn-save").ClickAsync();
        await ItShouldNestTheChildUnderItsParent();

        await page.GetByTestId("filter-search").FillAsync("ren");
        await ItShouldFilterTheTreeAsYouTypeAndKeepTheFilterInTheAddress();

        await page.GetByTestId("filter-search").FillAsync(string.Empty);
        await Expect(page.GetByTestId($"tree-node-{work}")).ToBeVisibleAsync();
        await page.GetByTestId($"btn-delete-{work}").ClickAsync();
        await ItShouldRemoveTheDeletedCategory();

        await page.GetByTestId("toggle-include-deleted").CheckAsync();
        await page.GetByTestId($"btn-restore-{work}").ClickAsync();
        await ItShouldBringTheCategoryBack();

        await page.GetByTestId("btn-add-root").ClickAsync();
        await page.Keyboard.PressAsync("Escape");
        await ItShouldCloseThePanelOnEscape();

        ItShouldNotHaveUpsetTheBrowser();

        async Task ItShouldKeepThePanelOpenAndShowWhatIsMissing()
        {
            await Expect(panel.GetByTestId("error-name")).ToHaveTextAsync("Name is required");
            await Expect(panel.GetByTestId("error-budgetFlow")).ToHaveTextAsync("Budget flow is required");
            await Expect(panel).ToBeVisibleAsync();
        }

        async Task ItShouldCloseThePanelConfirmAndShowTheCategory(string toast, string name)
        {
            await Expect(panel).ToHaveCountAsync(0);
            await Expect(page.GetByTestId("mnl-toast").Last).ToHaveTextAsync(toast);
            await Expect(tree).ToContainTextAsync(name);
        }

        async Task ItShouldNestTheChildUnderItsParent()
        {
            await Expect(panel).ToHaveCountAsync(0);
            await Expect(tree.Locator("li", new() { HasText = "Housing" }).Locator("ul li")).ToContainTextAsync("Rent");
            await Expect(page.Locator("[data-testid^='btn-view-items-']")).ToHaveCountAsync(2);
        }

        async Task ItShouldFilterTheTreeAsYouTypeAndKeepTheFilterInTheAddress()
        {
            await Expect(page.GetByTestId($"tree-node-{work}")).ToHaveCountAsync(0);
            await Expect(tree).ToContainTextAsync("Rent");
            await Expect(page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("[?&]q=ren"));
            await Expect(page.GetByTestId("filter-search")).ToBeFocusedAsync();
        }

        async Task ItShouldRemoveTheDeletedCategory()
        {
            await Expect(page.GetByTestId("mnl-toast").Last).ToHaveTextAsync("Category deleted.");
            await Expect(page.GetByTestId($"tree-node-{work}")).ToHaveCountAsync(0);
        }

        async Task ItShouldBringTheCategoryBack()
        {
            await Expect(page.GetByTestId("mnl-toast").Last).ToHaveTextAsync("Category restored.");
            await Expect(page.GetByTestId($"btn-delete-{work}")).ToBeVisibleAsync();
        }

        async Task ItShouldCloseThePanelOnEscape() => await Expect(panel).ToHaveCountAsync(0);

        void ItShouldNotHaveUpsetTheBrowser() => app.BrowserErrors.ShouldBeEmpty(string.Join(" || ", app.BrowserErrors));
    }

    [Test]
    public async Task GivenALeafCategory_WhenWorkingOnItsItems()
    {
        await using BudgetTestWebApplicationFactory api = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await api.CreateAntiforgeryClientAsync(cancellationToken: Cancellation);
        Guid budgetId = await UiHtml.CreateBudgetAsync(client, CurrentYear);
        Guid housing = await UiHtml.CreateCategoryAsync(client, budgetId, "Housing", "Expense");
        Guid rent = await UiHtml.CreateCategoryAsync(client, budgetId, "Rent", "Expense", housing);

        await using AppSession app = await browser.StartAsync(fixture.CreateIsolatedFactory(budgetItemsEnabled: true, keepData: true));
        IPage page = app.Page;
        ILocator createPanel = page.GetByTestId("single-create-panel");
        ILocator rows = page.Locator("[data-testid^='item-row-']");

        await page.GotoAsync($"/budgets/{budgetId}?month=3");
        await page.GetByTestId($"btn-view-items-{rent}").ClickAsync();
        await ItShouldOpenTheWorkspaceAndPutTheCategoryInTheAddress();

        await page.GetByTestId("btn-open-single-create").ClickAsync();
        await createPanel.GetByTestId("btn-add-attribution").ClickAsync();
        await createPanel.GetByTestId("input-attribution-percent").Last.FillAsync("25");
        await ItShouldKeepTheRunningTotalUpToDate("125%", onTarget: false);

        await createPanel.GetByTestId("btn-remove-attribution").Last.ClickAsync();
        await ItShouldKeepTheRunningTotalUpToDate("100%", onTarget: true);

        await createPanel.GetByTestId("input-month").SelectOptionAsync("3");
        await createPanel.GetByTestId("select-budgetFlow").SelectOptionAsync("Expense");
        await createPanel.GetByTestId("input-plannedAmount").FillAsync("2500,50");
        await createPanel.GetByTestId("btn-save").ClickAsync();
        await ItShouldAddTheItemAndUpdateTheSummary();

        await rows.First.GetByText("Record payment").ClickAsync();
        await page.GetByTestId("amount-form").GetByTestId("btn-submit").ClickAsync();
        await Expect(page.GetByTestId("error-amount")).ToHaveTextAsync("Amount is required and must be positive");
        await page.GetByTestId("input-amount").FillAsync("2400");
        await page.GetByTestId("amount-form").GetByTestId("btn-submit").ClickAsync();
        await ItShouldShowThePayment();

        await rows.First.GetByText("Fill forward").ClickAsync();
        await page.GetByTestId("amount-form").GetByTestId("btn-submit").ClickAsync();
        await Expect(rows).ToHaveCountAsync(10);

        await rows.First.GetByText("Delete").ClickAsync();
        await page.GetByTestId("btn-confirm-no").ClickAsync();
        await Expect(page.GetByTestId("delete-confirmation-panel")).ToHaveCountAsync(0);
        await Expect(rows).ToHaveCountAsync(10);

        await rows.First.GetByText("Delete").ClickAsync();
        await page.GetByTestId("btn-confirm-yes").ClickAsync();
        await ItShouldDeleteTheItemAfterConfirming();

        await page.GetByTestId($"btn-view-items-{rent}").ClickAsync();
        await ItShouldCloseTheWorkspace();

        ItShouldNotHaveUpsetTheBrowser();

        async Task ItShouldOpenTheWorkspaceAndPutTheCategoryInTheAddress()
        {
            await Expect(page.GetByTestId("category-title")).ToHaveTextAsync("Rent");
            await Expect(page.GetByTestId("state-empty")).ToBeVisibleAsync();
            await Expect(page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex($"category={rent}"));
            await Expect(page.GetByTestId($"btn-view-items-{rent}")).ToHaveTextAsync("Close items");
        }

        async Task ItShouldKeepTheRunningTotalUpToDate(string total, bool onTarget)
        {
            ILocator output = createPanel.GetByTestId("attribution-split-total");
            await Expect(output).ToHaveTextAsync(total);
            await Expect(output).ToHaveAttributeAsync("data-on-target", onTarget ? "true" : "false");
        }

        async Task ItShouldAddTheItemAndUpdateTheSummary()
        {
            await Expect(createPanel).ToHaveCountAsync(0);
            await Expect(page.GetByTestId("mnl-toast").Last).ToHaveTextAsync("Budget item created.");
            await Expect(rows).ToHaveCountAsync(1);
            await Expect(rows.First.GetByTestId("item-month")).ToHaveTextAsync("March");
            await Expect(rows.First.GetByTestId("planned-amount")).ToHaveTextAsync("R 2 500.50");
            await Expect(page.GetByTestId("summary-expenses")).ToContainTextAsync("2 500.50");
            await Expect(page.GetByTestId("summary-month")).ToHaveTextAsync("March");
        }

        async Task ItShouldShowThePayment()
        {
            await Expect(page.GetByTestId("mnl-toast").Last).ToHaveTextAsync("Payment recorded.");
            await Expect(rows.First.GetByTestId("spent-amount")).ToHaveTextAsync("R 2 400.00");
            await Expect(page.GetByTestId("summary-net").GetByTestId("summary-spent")).ToHaveTextAsync("-2 400.00");
        }

        async Task ItShouldDeleteTheItemAfterConfirming()
        {
            await Expect(page.GetByTestId("mnl-toast").Last).ToHaveTextAsync("Budget item deleted.");
            await Expect(rows).ToHaveCountAsync(9);
            await Expect(rows.First.GetByTestId("item-month")).ToHaveTextAsync("April");
        }

        async Task ItShouldCloseTheWorkspace()
        {
            await Expect(page.GetByTestId("budget-items-workspace")).ToHaveCountAsync(0);
            await Expect(page).Not.ToHaveURLAsync(new System.Text.RegularExpressions.Regex("category="));
        }

        void ItShouldNotHaveUpsetTheBrowser() => app.BrowserErrors.ShouldBeEmpty(string.Join(" || ", app.BrowserErrors));
    }

    [Test]
    public async Task GivenItemsInTwoMonths_WhenBrowsingTheSummary()
    {
        await using BudgetTestWebApplicationFactory api = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await api.CreateAntiforgeryClientAsync(cancellationToken: Cancellation);
        Guid budgetId = await UiHtml.CreateBudgetAsync(client, CurrentYear);
        Guid housing = await UiHtml.CreateCategoryAsync(client, budgetId, "Housing", "Expense");
        Guid rent = await UiHtml.CreateCategoryAsync(client, budgetId, "Rent", "Expense", housing);
        await UiHtml.CreateItemAsync(client, budgetId, rent, "Expense", 3, 100m);
        await UiHtml.CreateItemAsync(client, budgetId, rent, "Expense", 4, 250m);

        await using AppSession app = await browser.StartAsync(fixture.CreateIsolatedFactory(budgetItemsEnabled: true, keepData: true));
        IPage page = app.Page;
        ILocator expenses = page.GetByTestId("summary-expenses");

        await page.GotoAsync($"/budgets/{budgetId}?month=3");
        await Expect(expenses).ToContainTextAsync("100.00");
        await page.EvaluateAsync("() => { window.__sameDocument = true; window.scrollTo(0, 300); }");
        await page.GetByTestId("summary-next-month").ScrollIntoViewIfNeededAsync();
        int scrolledTo = await page.EvaluateAsync<int>("() => Math.round(window.scrollY)");

        await page.GetByTestId("summary-next-month").ClickAsync();
        await ItShouldShowAprilInPlace();

        await page.GetByTestId("summary-views").GetByText("Yearly").ClickAsync();
        await ItShouldShowTheWholeYear();

        await page.ReloadAsync();
        await ItShouldStillShowTheWholeYearAfterAReload();

        ItShouldNotHaveUpsetTheBrowser();

        async Task ItShouldShowAprilInPlace()
        {
            await Expect(page.GetByTestId("summary-month")).ToHaveTextAsync("April");
            await Expect(expenses).ToContainTextAsync("250.00");
            await Expect(page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("month=4"));
            (await page.EvaluateAsync<bool>("() => window.__sameDocument === true")).ShouldBeTrue();
            scrolledTo.ShouldBeGreaterThan(0);
            (await page.EvaluateAsync<int>("() => Math.round(window.scrollY)")).ShouldBe(scrolledTo);
        }

        async Task ItShouldShowTheWholeYear()
        {
            await Expect(page.GetByTestId("summary-month")).ToHaveCountAsync(0);
            await Expect(expenses).ToContainTextAsync("350.00");
        }

        async Task ItShouldStillShowTheWholeYearAfterAReload()
        {
            await Expect(expenses).ToContainTextAsync("350.00");
            await Expect(page.GetByTestId("summary-views").Locator("[aria-current]")).ToHaveTextAsync("Yearly");
        }

        void ItShouldNotHaveUpsetTheBrowser() => app.BrowserErrors.ShouldBeEmpty(string.Join(" || ", app.BrowserErrors));
    }

    [Test]
    public async Task GivenTheDesignGallery_WhenUsingItsLiveExamples()
    {
        BudgetTestWebApplicationFactory factory = new()
        {
            MenloConnectionString = fixture.ConnectionString,
            UseRealUserProvisioning = true,
            ConfigurationOverrides = new Dictionary<string, string?> { ["Features:DesignGallery"] = "true" }
        };
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
            fixture.CreateIsolatedFactory(budgetItemsEnabled: true),
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
