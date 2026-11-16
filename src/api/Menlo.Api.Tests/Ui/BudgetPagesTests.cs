using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Menlo.Api.Tests.Budget;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;

namespace Menlo.Api.Tests.Ui;

/// <summary>
/// The budget list and budget detail pages, rendered from a real database.
/// </summary>
[ClassDataSource<BudgetApiFixture>(Shared = SharedType.PerTestSession)]
[NotInParallel("Budget")]
public sealed class BudgetPagesTests(BudgetApiFixture fixture) : TestFixture
{
    private static readonly WebApplicationFactoryClientOptions NoRedirects = new() { AllowAutoRedirect = false };

    private static CancellationToken Cancellation => TestContext.Current!.Execution.CancellationToken;

    private static int CurrentYear => DateTimeOffset.Now.Year;

    private static int CurrentMonth => DateTimeOffset.Now.Month;

    [Test]
    public async Task GivenNoBudgets_WhenOpeningTheBudgetList()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(NoRedirects, Cancellation);

        IHtmlDocument document = await (await client.GetAsync("/budgets", Cancellation)).DocumentAsync();

        ItShouldShowTheEmptyStateWithAButtonForThisYear();
        ItShouldMarkBudgetsAsTheCurrentSection();

        void ItShouldShowTheEmptyStateWithAButtonForThisYear()
        {
            IElement empty = document.ByTestId("budget-list-empty-state");
            empty.TextContent.ShouldContain("No budgets yet");
            IElement form = document.ByTestId("empty-create-budget-button").Closest("form")!;
            form.GetAttribute("action").ShouldBe($"/budgets/{CurrentYear}");
            form.GetAttribute("method").ShouldBe("post");
        }

        void ItShouldMarkBudgetsAsTheCurrentSection() =>
            document.ByTestId("mnl-nav-item-/budgets").GetAttribute("aria-current").ShouldBe("page");
    }

    [Test]
    public async Task GivenNoBudgets_WhenCreatingThisYearsBudget()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(NoRedirects, Cancellation);

        HttpResponseMessage created = await client.HtmxAsync(HttpMethod.Post, $"/budgets/{CurrentYear}");
        string location = created.Headers.GetValues("HX-Location").Single();
        string detailUrl = System.Text.Json.JsonDocument.Parse(location).RootElement.GetProperty("path").GetString()!;
        IHtmlDocument detail = await (await client.GetAsync(detailUrl, Cancellation)).DocumentAsync();
        IHtmlDocument again = await (await client.GetAsync(detailUrl, Cancellation)).DocumentAsync();
        IHtmlDocument list = await (await client.GetAsync("/budgets", Cancellation)).DocumentAsync();

        ItShouldSendTheBrowserToTheNewBudget();
        ItShouldConfirmWithAToastOnThatPageOnce();
        ItShouldOfferNextYearOnTheListAndTheDetailPage();

        void ItShouldSendTheBrowserToTheNewBudget()
        {
            created.StatusCode.ShouldBe(HttpStatusCode.NoContent);
            detailUrl.ShouldStartWith("/budgets/");
            detail.ByTestId("budget-year").TextContent.ShouldBe($"{CurrentYear} Budget");
            detail.ByTestId("status-badge").TextContent.Trim().ShouldBe("Draft");
        }

        void ItShouldConfirmWithAToastOnThatPageOnce()
        {
            detail.QuerySelector("#mnl-toast-region > [data-mnl-toast]")!.TextContent.ShouldContain($"Budget {CurrentYear} created.");
            again.QuerySelector("#mnl-toast-region > [data-mnl-toast]").ShouldBeNull();
        }

        void ItShouldOfferNextYearOnTheListAndTheDetailPage()
        {
            list.ByTestId("create-budget-button").TextContent.ShouldContain($"Create {CurrentYear + 1} Budget");
            detail.ByTestId("create-next-year-btn").Closest("form")!.GetAttribute("action").ShouldBe($"/budgets/{CurrentYear + 1}");
        }
    }

    [Test]
    public async Task GivenAYearThatCanNotHaveABudget_WhenCreatingIt()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(NoRedirects, Cancellation);

        HttpResponseMessage response = await client.HtmxAsync(HttpMethod.Post, "/budgets/1800");
        IHtmlDocument document = await response.DocumentAsync();

        ItShouldShowTheListAgainWithTheReason();

        void ItShouldShowTheListAgainWithTheReason()
        {
            response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
            document.ByTestId("create-budget-error").TextContent.ShouldNotBeNullOrWhiteSpace();
            document.ByTestId("budget-list-page").ShouldNotBeNull();
        }
    }

    [Test]
    public async Task GivenABudgetWithSpending_WhenOpeningTheBudgetList()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(NoRedirects, Cancellation);
        Guid budgetId = await UiHtml.CreateBudgetAsync(client, CurrentYear);
        Guid housing = await UiHtml.CreateCategoryAsync(client, budgetId, "Housing", "Expense");
        Guid rent = await UiHtml.CreateCategoryAsync(client, budgetId, "Rent", "Expense", housing);
        Guid item = await UiHtml.CreateItemAsync(client, budgetId, rent, "Expense", CurrentMonth, 2000m);
        await client.HtmxAsync(HttpMethod.Put, $"/budgets/{budgetId}/categories/{rent}/items/{item}/spent", [new("amount", "1700")]);

        IHtmlDocument document = await (await client.GetAsync("/budgets", Cancellation)).DocumentAsync();

        ItShouldShowTheBudgetAsACard();
        ItShouldShowThisMonthsSpendingAsAWarning();
        ItShouldLinkToTheDetailPage();

        void ItShouldShowTheBudgetAsACard()
        {
            IElement card = document.ByTestId($"budget-card-{budgetId}");
            card.TextContent.ShouldContain($"Budget {CurrentYear}");
            card.TextContent.ShouldContain("2 categories");
            document.ByTestId($"budget-status-{budgetId}").TextContent.Trim().ShouldBe("Draft");
        }

        void ItShouldShowThisMonthsSpendingAsAWarning()
        {
            IElement progress = document.ByTestId($"budget-progress-{budgetId}");
            progress.GetAttribute("data-tone").ShouldBe("warning");
            progress.QuerySelector("progress")!.GetAttribute("value").ShouldBe("85");
            document.ByTestId($"budget-spend-{budgetId}").TextContent.ShouldContain("R 1 700.00 spent of R 2 000.00 planned this month");
            document.ByTestId($"budget-spend-{budgetId}").TextContent.ShouldContain("85%");
        }

        void ItShouldLinkToTheDetailPage() =>
            document.ByTestId($"budget-view-{budgetId}").GetAttribute("href").ShouldBe($"/budgets/{budgetId}");
    }

    [Test]
    public async Task GivenABudgetWithCategoriesAndItems_WhenOpeningItsDetailPage()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(NoRedirects, Cancellation);
        Guid budgetId = await UiHtml.CreateBudgetAsync(client, CurrentYear);
        Guid income = await UiHtml.CreateCategoryAsync(client, budgetId, "Work", "Income");
        Guid salary = await UiHtml.CreateCategoryAsync(client, budgetId, "Salary", "Income", income);
        Guid housing = await UiHtml.CreateCategoryAsync(client, budgetId, "Housing", "Expense", attribution: "Rental");
        Guid rent = await UiHtml.CreateCategoryAsync(client, budgetId, "Rent", "Expense", housing);
        await UiHtml.CreateItemAsync(client, budgetId, salary, "Income", CurrentMonth, 9000m);
        Guid rentItem = await UiHtml.CreateItemAsync(client, budgetId, rent, "Expense", CurrentMonth, 2500m);

        IHtmlDocument page = await (await client.GetAsync($"/budgets/{budgetId}", Cancellation)).DocumentAsync();
        IHtmlDocument withItems = await (await client.GetAsync($"/budgets/{budgetId}?category={rent}", Cancellation)).DocumentAsync();
        IHtmlDocument withParent = await (await client.GetAsync($"/budgets/{budgetId}?category={housing}", Cancellation)).DocumentAsync();

        ItShouldShowTheNetPlannedForThisMonth();
        ItShouldShowTheCategoryTreeWithItsActions();
        ItShouldListTheCategoriesAndOfferItemsOnlyForLeaves();
        ItShouldShowTheMonthlySummary();
        ItShouldReloadItsRegionWhenBudgetDataChanges();
        ItShouldOpenTheItemsOfTheCategoryInTheAddress();
        ItShouldIgnoreACategoryThatIsNotALeaf();

        void ItShouldShowTheNetPlannedForThisMonth() =>
            page.ByTestId("total-amount").TextContent.ShouldBe("R 6 500.00");

        void ItShouldShowTheCategoryTreeWithItsActions()
        {
            IElement housingNode = page.ByTestId($"tree-node-{housing}");
            housingNode.TextContent.ShouldContain("Housing");
            housingNode.TextContent.ShouldContain("Rental");
            housingNode.FindByTestId($"tree-node-{rent}").ShouldNotBeNull();
            page.ByTestId("btn-add-root").GetAttribute("hx-get").ShouldBe($"/budgets/{budgetId}/categories/new");
            page.ByTestId($"btn-add-child-{housing}").GetAttribute("hx-get").ShouldBe($"/budgets/{budgetId}/categories/new?parentId={housing}");
            page.ByTestId($"btn-edit-{rent}").GetAttribute("hx-target").ShouldBe("#mnl-panel-host");
            page.ByTestId($"btn-delete-{rent}").GetAttribute("hx-delete").ShouldBe($"/budgets/{budgetId}/categories/{rent}");
        }

        void ItShouldListTheCategoriesAndOfferItemsOnlyForLeaves()
        {
            page.ByTestId($"category-{rent}").GetAttribute("data-depth").ShouldBe("1");
            IElement view = page.ByTestId($"btn-view-items-{rent}");
            view.TextContent.Trim().ShouldBe("View items");
            view.GetAttribute("href")!.ShouldContain($"category={rent}");
            view.GetAttribute("hx-select").ShouldBe("#budget-detail");
            page.FindByTestId($"btn-view-items-{housing}").ShouldBeNull();
            page.FindByTestId("items-workspace-section").ShouldBeNull();
        }

        void ItShouldShowTheMonthlySummary()
        {
            page.ByTestId("summary-month").TextContent.ShouldNotBeNullOrWhiteSpace();
            page.ByTestId("summary-income").TextContent.ShouldContain("9 000.00");
            page.ByTestId("summary-expenses").TextContent.ShouldContain("Rent");
            page.ByTestId("summary-net").ByTestId("summary-planned").TextContent.ShouldBe("6 500.00");
            page.ByTestId("summary-net").FindByTestId("summary-spent").ShouldBeNull();
        }

        void ItShouldReloadItsRegionWhenBudgetDataChanges()
        {
            IElement region = page.QuerySelector("#budget-detail")!;
            region.GetAttribute("hx-trigger").ShouldBe("categories-changed from:body, items-changed from:body");
            region.GetAttribute("hx-get").ShouldBe(string.Empty);
            region.GetAttribute("hx-select").ShouldBe("#budget-detail");
        }

        void ItShouldOpenTheItemsOfTheCategoryInTheAddress()
        {
            withItems.ByTestId("category-title").TextContent.ShouldBe("Rent");
            IElement row = withItems.ByTestId($"item-row-{rentItem}");
            row.ByTestId("planned-amount").TextContent.ShouldBe("R 2 500.00");
            row.ByTestId("item-flow").TextContent.Trim().ShouldBe("Expense");
            row.FindByTestId("spent-amount").ShouldBeNull();
            withItems.ByTestId($"btn-view-items-{rent}").TextContent.Trim().ShouldBe("Close items");
            withItems.ByTestId($"btn-view-items-{rent}").GetAttribute("href")!.ShouldNotContain("category=");
            withItems.ByTestId("btn-open-single-create").GetAttribute("hx-get").ShouldBe($"/budgets/{budgetId}/categories/{rent}/items/new");
        }

        void ItShouldIgnoreACategoryThatIsNotALeaf() =>
            withParent.FindByTestId("items-workspace-section").ShouldBeNull();
    }

    [Test]
    public async Task GivenFiltersAndAPeriodInTheAddress_WhenOpeningTheDetailPage()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(NoRedirects, Cancellation);
        Guid budgetId = await UiHtml.CreateBudgetAsync(client, CurrentYear);
        Guid housing = await UiHtml.CreateCategoryAsync(client, budgetId, "Housing", "Expense");
        Guid rent = await UiHtml.CreateCategoryAsync(client, budgetId, "Rent", "Expense", housing);
        Guid work = await UiHtml.CreateCategoryAsync(client, budgetId, "Work", "Income");
        Guid old = await UiHtml.CreateCategoryAsync(client, budgetId, "Old", "Expense");
        await client.HtmxAsync(HttpMethod.Delete, $"/budgets/{budgetId}/categories/{old}");
        await UiHtml.CreateItemAsync(client, budgetId, rent, "Expense", 3, 100m);
        await UiHtml.CreateItemAsync(client, budgetId, rent, "Expense", 4, 250m);

        IHtmlDocument searched = await (await client.GetAsync($"/budgets/{budgetId}?q=ren", Cancellation)).DocumentAsync();
        IHtmlDocument byFlow = await (await client.GetAsync($"/budgets/{budgetId}?flow=Income", Cancellation)).DocumentAsync();
        IHtmlDocument nothing = await (await client.GetAsync($"/budgets/{budgetId}?q=zzz", Cancellation)).DocumentAsync();
        IHtmlDocument withDeleted = await (await client.GetAsync($"/budgets/{budgetId}?deleted=true", Cancellation)).DocumentAsync();
        IHtmlDocument march = await (await client.GetAsync($"/budgets/{budgetId}?view=monthly&month=3", Cancellation)).DocumentAsync();
        IHtmlDocument yearly = await (await client.GetAsync($"/budgets/{budgetId}?view=yearly&month=3", Cancellation)).DocumentAsync();
        IHtmlDocument december = await (await client.GetAsync($"/budgets/{budgetId}?month=12", Cancellation)).DocumentAsync();

        ItShouldKeepAMatchAndTheParentsAboveIt();
        ItShouldFilterByFlow();
        ItShouldSayWhenNothingMatches();
        ItShouldShowDeletedCategoriesOnlyWhenAsked();
        ItShouldSummariseTheMonthInTheAddress();
        ItShouldSummariseTheWholeYear();
        ItShouldStopAtDecember();

        void ItShouldKeepAMatchAndTheParentsAboveIt()
        {
            searched.FindByTestId($"tree-node-{rent}").ShouldNotBeNull();
            searched.FindByTestId($"tree-node-{housing}").ShouldNotBeNull();
            searched.FindByTestId($"tree-node-{work}").ShouldBeNull();
            searched.ByTestId("filter-search").GetAttribute("value").ShouldBe("ren");
        }

        void ItShouldFilterByFlow()
        {
            byFlow.FindByTestId($"tree-node-{work}").ShouldNotBeNull();
            byFlow.FindByTestId($"tree-node-{housing}").ShouldBeNull();
        }

        void ItShouldSayWhenNothingMatches() =>
            nothing.ByTestId("tree-empty").TextContent.ShouldContain("No categories found.");

        void ItShouldShowDeletedCategoriesOnlyWhenAsked()
        {
            searched.FindByTestId($"tree-node-{old}").ShouldBeNull();
            IElement deleted = withDeleted.ByTestId($"tree-node-{old}");
            deleted.TextContent.ShouldContain("Deleted");
            withDeleted.ByTestId($"btn-restore-{old}").GetAttribute("hx-put").ShouldBe($"/budgets/{budgetId}/categories/{old}/restore");
            withDeleted.ByTestId("toggle-include-deleted").HasAttribute("checked").ShouldBeTrue();
        }

        void ItShouldSummariseTheMonthInTheAddress()
        {
            march.ByTestId("summary-month").TextContent.ShouldBe("March");
            march.ByTestId("summary-expenses").TextContent.ShouldContain("100.00");
            march.ByTestId("summary-previous-month").GetAttribute("href")!.ShouldContain("month=2");
            march.ByTestId("summary-next-month").GetAttribute("href")!.ShouldContain("month=4");
            march.ByTestId("summary-views").QuerySelector("[aria-current]")!.TextContent.ShouldBe("Monthly");
        }

        void ItShouldSummariseTheWholeYear()
        {
            yearly.FindByTestId("summary-month").ShouldBeNull();
            yearly.ByTestId("summary-expenses").TextContent.ShouldContain("350.00");
            yearly.ByTestId("summary-views").QuerySelector("[aria-current]")!.TextContent.ShouldBe("Yearly");
        }

        void ItShouldStopAtDecember()
        {
            IElement next = december.ByTestId("summary-next-month");
            next.HasAttribute("href").ShouldBeFalse();
            next.HasAttribute("hx-get").ShouldBeFalse();
            next.HasAttribute("disabled").ShouldBeTrue();
        }
    }

    [Test]
    public async Task GivenAReader_WhenOpeningTheBudgetPages()
    {
        await using BudgetTestWebApplicationFactory editor = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient editorClient = await editor.CreateAntiforgeryClientAsync(NoRedirects, Cancellation);
        Guid budgetId = await UiHtml.CreateBudgetAsync(editorClient, CurrentYear);
        Guid rent = await UiHtml.CreateCategoryAsync(editorClient, budgetId, "Rent", "Expense");
        await UiHtml.CreateItemAsync(editorClient, budgetId, rent, "Expense", CurrentMonth, 2500m);

        await using BudgetTestWebApplicationFactory factory =
            fixture.CreateIsolatedFactory(budgetItemsEnabled: true, keepData: true, userRoles: ["Menlo.Reader"]);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(NoRedirects, Cancellation);

        IHtmlDocument list = await (await client.GetAsync("/budgets", Cancellation)).DocumentAsync();
        IHtmlDocument detail = await (await client.GetAsync($"/budgets/{budgetId}?category={rent}", Cancellation)).DocumentAsync();
        HttpResponseMessage form = await client.HtmxAsync(HttpMethod.Get, $"/budgets/{budgetId}/categories/new");
        HttpResponseMessage delete = await client.HtmxAsync(HttpMethod.Delete, $"/budgets/{budgetId}/categories/{rent}");

        ItShouldShowTheBudgetsWithoutAnyWayToChangeThem();
        ItShouldRefuseTheFormsAndActions();

        void ItShouldShowTheBudgetsWithoutAnyWayToChangeThem()
        {
            list.ByTestId($"budget-card-{budgetId}").ShouldNotBeNull();
            list.FindByTestId("create-budget-button").ShouldBeNull();
            detail.ByTestId("category-title").TextContent.ShouldBe("Rent");
            detail.QuerySelectorAll("[hx-get*='/new'], [hx-get$='/edit'], [hx-delete], [hx-put], [hx-post]").ShouldBeEmpty();
            detail.FindByTestId("create-next-year-btn").ShouldBeNull();
        }

        void ItShouldRefuseTheFormsAndActions()
        {
            form.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
            delete.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        }
    }

    [Test]
    public async Task GivenABudgetThatDoesNotExist_WhenOpeningItsDetailPage()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = factory.CreateClient(NoRedirects);

        HttpResponseMessage response = await client.GetAsync($"/budgets/{Guid.NewGuid()}", Cancellation);
        IHtmlDocument document = await response.DocumentAsync();

        ItShouldSayItCouldNotBeLoadedAndOfferAWayBack();

        void ItShouldSayItCouldNotBeLoadedAndOfferAWayBack()
        {
            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            document.ByTestId("error-banner").TextContent.ShouldContain("We couldn't load this budget");
            document.QuerySelector("a[href='/budgets'].mnl-button").ShouldNotBeNull();
        }
    }

    [Test]
    public async Task GivenBudgetItemsAreSwitchedOff_WhenOpeningTheDetailPage()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: false);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(NoRedirects, Cancellation);
        Guid budgetId = await UiHtml.CreateBudgetAsync(client, CurrentYear);
        Guid rent = await UiHtml.CreateCategoryAsync(client, budgetId, "Rent", "Expense");

        IHtmlDocument document = await (await client.GetAsync($"/budgets/{budgetId}?category={rent}", Cancellation)).DocumentAsync();

        ItShouldStillShowTheCategories();
        ItShouldExplainWhyTheSummaryAndItemsAreMissing();

        void ItShouldStillShowTheCategories() => document.ByTestId($"tree-node-{rent}").ShouldNotBeNull();

        void ItShouldExplainWhyTheSummaryAndItemsAreMissing()
        {
            document.ByTestId("summary-error").TextContent.ShouldContain("isn't switched on");
            document.ByTestId("state-error").TextContent.ShouldContain("isn't switched on");
        }
    }

    [Test]
    public async Task GivenTheHomePage_WhenItsBudgetWidgetLoads()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(NoRedirects, Cancellation);

        HttpResponseMessage response = await client.HtmxAsync(HttpMethod.Post, "/home/budget-widget");
        IHtmlDocument widget = await response.DocumentAsync();
        IHtmlDocument list = await (await client.GetAsync("/budgets", Cancellation)).DocumentAsync();

        ItShouldMakeSureThisYearsBudgetExistsAndShowIt();

        void ItShouldMakeSureThisYearsBudgetExistsAndShowIt()
        {
            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            widget.ByTestId("widget-title").TextContent.ShouldBe($"Budget {CurrentYear}");
            widget.ByTestId("widget-status").TextContent.Trim().ShouldBe("Draft");
            widget.ByTestId("widget-total").TextContent.ShouldContain("R 0.00");
            string href = widget.ByTestId("view-budget-button").GetAttribute("href")!;
            list.QuerySelector($"a[href='{href}']").ShouldNotBeNull();
        }
    }

    [Test]
    public async Task GivenTheBudgetFeatureIsOff_WhenTheHomeWidgetLoads()
    {
        await using TestWebApplicationFactory factory = new BudgetTestWebApplicationFactory
        {
            MenloConnectionString = fixture.ConnectionString,
            UseRealUserProvisioning = true
        };
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(NoRedirects, Cancellation);

        IHtmlDocument widget = await (await client.HtmxAsync(HttpMethod.Post, "/home/budget-widget")).DocumentAsync();
        IHtmlDocument list = await (await client.GetAsync("/budgets", Cancellation)).DocumentAsync();

        ItShouldSayTheBudgetCouldNotBeLoaded();

        void ItShouldSayTheBudgetCouldNotBeLoaded()
        {
            widget.ByTestId("widget-error").TextContent.ShouldContain("We couldn't load this year's budget.");
            widget.ByTestId("view-budget-button").GetAttribute("aria-disabled").ShouldBe("true");
            list.ByTestId("budget-list-error").ShouldNotBeNull();
        }
    }
}
