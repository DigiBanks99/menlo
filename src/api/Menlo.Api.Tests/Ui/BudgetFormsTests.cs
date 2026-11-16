using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Menlo.Api.Tests.Budget;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;

namespace Menlo.Api.Tests.Ui;

/// <summary>
/// The category and budget item forms: the panels they open in, validation, and the events a
/// successful change sends so the page reloads itself.
/// </summary>
[ClassDataSource<BudgetApiFixture>(Shared = SharedType.PerTestSession)]
[NotInParallel("Budget")]
public sealed class BudgetFormsTests(BudgetApiFixture fixture) : TestFixture
{
    private static readonly WebApplicationFactoryClientOptions NoRedirects = new() { AllowAutoRedirect = false };

    private static CancellationToken Cancellation => TestContext.Current!.Execution.CancellationToken;

    private static int CurrentYear => DateTimeOffset.Now.Year;

    [Test]
    public async Task GivenABudget_WhenAddingARootCategory()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(NoRedirects, Cancellation);
        Guid budgetId = await UiHtml.CreateBudgetAsync(client, CurrentYear);
        string categories = $"/budgets/{budgetId}/categories";

        IHtmlDocument panel = await (await client.HtmxAsync(HttpMethod.Get, $"{categories}/new")).DocumentAsync();
        HttpResponseMessage invalid = await client.HtmxAsync(HttpMethod.Post, categories, [new("name", "  "), new("budgetFlow", "")]);
        IHtmlDocument invalidForm = await invalid.DocumentAsync();
        HttpResponseMessage created = await client.HtmxAsync(HttpMethod.Post, categories,
        [
            new("name", "Housing"), new("budgetFlow", "Expense"), new("attribution", "Main"), new("description", "Home costs")
        ]);
        HttpResponseMessage duplicate = await client.HtmxAsync(HttpMethod.Post, categories, [new("name", "Housing"), new("budgetFlow", "Expense")]);
        IHtmlDocument duplicateForm = await duplicate.DocumentAsync();
        IHtmlDocument page = await (await client.GetAsync($"/budgets/{budgetId}", Cancellation)).DocumentAsync();

        ItShouldOpenTheFormInAPanel();
        ItShouldSendTheFormBackWithItsErrors();
        ItShouldCloseThePanelConfirmAndAskThePageToReload();
        ItShouldPutADuplicateNameOnTheNameField();
        ItShouldShowTheNewCategoryOnThePage();

        void ItShouldOpenTheFormInAPanel()
        {
            IElement dialog = panel.ByTestId("category-form-panel");
            dialog.HasAttribute("data-mnl-panel").ShouldBeTrue();
            dialog.TextContent.ShouldContain("Add a new top-level category to this budget.");
            IElement form = panel.ByTestId("category-form");
            form.GetAttribute("hx-post").ShouldBe(categories);
            form.GetAttribute("hx-target").ShouldBe("this");
            form.GetAttribute("hx-swap").ShouldBe("outerHTML");
            form.QuerySelector("input[name='parentId']").ShouldBeNull();
        }

        void ItShouldSendTheFormBackWithItsErrors()
        {
            invalid.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
            invalidForm.QuerySelector("dialog").ShouldBeNull();
            invalidForm.ByTestId("error-name").TextContent.ShouldBe("Name is required");
            invalidForm.ByTestId("error-budgetFlow").TextContent.ShouldBe("Budget flow is required");
            invalidForm.ByTestId("input-name").GetAttribute("aria-invalid").ShouldBe("true");
        }

        void ItShouldCloseThePanelConfirmAndAskThePageToReload()
        {
            created.StatusCode.ShouldBe(HttpStatusCode.NoContent);
            created.Triggers().ShouldContain("\"message\":\"Category created.\"");
            created.Triggers().ShouldContain("\"mnl:close-panel\":true");
            created.Triggers().ShouldContain("\"categories-changed\":true");
        }

        void ItShouldPutADuplicateNameOnTheNameField()
        {
            duplicate.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
            duplicateForm.ByTestId("error-name").TextContent.ShouldNotBeNullOrWhiteSpace();
            duplicateForm.ByTestId("input-name").GetAttribute("value").ShouldBe("Housing");
        }

        void ItShouldShowTheNewCategoryOnThePage()
        {
            IElement node = page.QuerySelector("[data-testid^='tree-node-']")!;
            node.TextContent.ShouldContain("Housing");
            node.TextContent.ShouldContain("Expense");
            node.TextContent.ShouldContain("Main");
        }
    }

    [Test]
    public async Task GivenACategory_WhenAddingAChildEditingDeletingAndRestoringIt()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(NoRedirects, Cancellation);
        Guid budgetId = await UiHtml.CreateBudgetAsync(client, CurrentYear);
        Guid housing = await UiHtml.CreateCategoryAsync(client, budgetId, "Housing", "Expense");
        string categories = $"/budgets/{budgetId}/categories";

        IHtmlDocument childPanel = await (await client.HtmxAsync(HttpMethod.Get, $"{categories}/new?parentId={housing}")).DocumentAsync();
        HttpResponseMessage child = await client.HtmxAsync(HttpMethod.Post, categories,
        [
            new("name", "Rent"), new("budgetFlow", "Expense"), new("parentId", housing.ToString())
        ]);
        IHtmlDocument afterChild = await (await client.GetAsync($"/budgets/{budgetId}", Cancellation)).DocumentAsync();
        string rentTestId = afterChild.ByTestId($"tree-node-{housing}").QuerySelector("ul [data-testid^='tree-node-']")!.GetAttribute("data-testid")!;
        string rent = $"{categories}/{rentTestId["tree-node-".Length..]}";

        IHtmlDocument editPanel = await (await client.HtmxAsync(HttpMethod.Get, $"{rent}/edit")).DocumentAsync();
        HttpResponseMessage invalidUpdate = await client.HtmxAsync(HttpMethod.Put, rent, [new("name", ""), new("budgetFlow", "Expense")]);
        HttpResponseMessage updated = await client.HtmxAsync(HttpMethod.Put, rent,
        [
            new("name", "Rent and levies"), new("budgetFlow", "Expense"), new("attribution", "Rental"), new("responsiblePayer", "Sam")
        ]);
        IHtmlDocument afterUpdate = await (await client.GetAsync($"/budgets/{budgetId}", Cancellation)).DocumentAsync();
        HttpResponseMessage deleted = await client.HtmxAsync(HttpMethod.Delete, rent);
        IHtmlDocument afterDelete = await (await client.GetAsync($"/budgets/{budgetId}", Cancellation)).DocumentAsync();
        HttpResponseMessage restored = await client.HtmxAsync(HttpMethod.Put, $"{rent}/restore");
        IHtmlDocument afterRestore = await (await client.GetAsync($"/budgets/{budgetId}", Cancellation)).DocumentAsync();
        HttpResponseMessage missing = await client.HtmxAsync(HttpMethod.Get, $"{categories}/{Guid.NewGuid()}/edit");

        ItShouldCarryTheParentInTheChildForm();
        ItShouldNestTheChildUnderItsParent();
        ItShouldOpenTheEditFormWithTheCurrentValues();
        ItShouldValidateAnUpdate();
        ItShouldSaveTheUpdate();
        ItShouldDeleteAndRestore();
        ItShouldSayWhenTheCategoryToEditIsGone();

        void ItShouldCarryTheParentInTheChildForm()
        {
            childPanel.QuerySelector("input[name='parentId']")!.GetAttribute("value").ShouldBe(housing.ToString());
            childPanel.ByTestId("category-form-panel").TextContent.ShouldContain("Add child category");
        }

        void ItShouldNestTheChildUnderItsParent()
        {
            child.StatusCode.ShouldBe(HttpStatusCode.NoContent);
            afterChild.ByTestId($"tree-node-{housing}").TextContent.ShouldContain("Rent");
        }

        void ItShouldOpenTheEditFormWithTheCurrentValues()
        {
            IElement form = editPanel.ByTestId("category-form");
            form.GetAttribute("hx-put").ShouldBe(rent);
            form.ByTestId("input-name").GetAttribute("value").ShouldBe("Rent");
            form.ByTestId("select-budgetFlow").QuerySelector("option[selected]")!.GetAttribute("value").ShouldBe("Expense");
            form.ByTestId("btn-save").TextContent.Trim().ShouldBe("Update");
        }

        void ItShouldValidateAnUpdate() => invalidUpdate.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);

        void ItShouldSaveTheUpdate()
        {
            updated.StatusCode.ShouldBe(HttpStatusCode.NoContent);
            updated.Triggers().ShouldContain("Category updated.");
            IElement node = afterUpdate.ByTestId(rentTestId);
            node.TextContent.ShouldContain("Rent and levies");
            node.TextContent.ShouldContain("Rental");
        }

        void ItShouldDeleteAndRestore()
        {
            deleted.StatusCode.ShouldBe(HttpStatusCode.NoContent);
            deleted.Triggers().ShouldContain("Category deleted.");
            deleted.Triggers().ShouldContain("\"categories-changed\":true");
            afterDelete.FindByTestId(rentTestId).ShouldBeNull();
            restored.Triggers().ShouldContain("Category restored.");
            afterRestore.FindByTestId(rentTestId).ShouldNotBeNull();
        }

        void ItShouldSayWhenTheCategoryToEditIsGone()
        {
            missing.StatusCode.ShouldBe(HttpStatusCode.NoContent);
            missing.Triggers().ShouldContain("\"tone\":\"error\"");
        }
    }

    [Test]
    public async Task GivenALeafCategory_WhenAddingABudgetItem()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(NoRedirects, Cancellation);
        Guid budgetId = await UiHtml.CreateBudgetAsync(client, CurrentYear);
        Guid rent = await UiHtml.CreateCategoryAsync(client, budgetId, "Rent", "Expense");
        string items = $"/budgets/{budgetId}/categories/{rent}/items";

        IHtmlDocument panel = await (await client.HtmxAsync(HttpMethod.Get, $"{items}/new")).DocumentAsync();
        string member = panel.QuerySelector("select[name='payerUserId'] option[selected]")!.GetAttribute("value")!;

        HttpResponseMessage invalid = await client.HtmxAsync(HttpMethod.Post, items,
        [
            new("month", "13"), new("budgetFlow", ""), new("plannedAmount", "lots"),
            new("payerUserId", member), new("payerPercent", "60"),
            new("attribution", "Main"), new("attributionPercent", "50"),
            new("attribution", "Rental"), new("attributionPercent", "30")
        ]);
        IHtmlDocument invalidForm = await invalid.DocumentAsync();

        HttpResponseMessage noPayer = await client.HtmxAsync(HttpMethod.Post, items,
        [
            new("month", "3"), new("budgetFlow", "Expense"), new("plannedAmount", "100"),
            new("payerUserId", ""), new("payerPercent", "100"),
            new("attribution", "Elsewhere"), new("attributionPercent", "100")
        ]);
        IHtmlDocument noPayerForm = await noPayer.DocumentAsync();

        KeyValuePair<string, string>[] valid =
        [
            new("month", "3"), new("budgetFlow", "Expense"), new("plannedAmount", "2 500,50"),
            new("payerUserId", member), new("payerPercent", "100"),
            new("attribution", "Main"), new("attributionPercent", "100")
        ];
        HttpResponseMessage created = await client.HtmxAsync(HttpMethod.Post, items, valid);
        HttpResponseMessage again = await client.HtmxAsync(HttpMethod.Post, items, valid);
        IHtmlDocument againForm = await again.DocumentAsync();
        IHtmlDocument page = await (await client.GetAsync($"/budgets/{budgetId}?category={rent}", Cancellation)).DocumentAsync();

        ItShouldOpenTheFormWithACompleteSplitToStartFrom();
        ItShouldSendTheFormBackWithEveryError();
        ItShouldAskForAPersonAndAKnownAttribution();
        ItShouldSaveCloseThePanelAndAskThePageToReload();
        ItShouldShowWhyASecondItemForTheSameMonthIsRefused();
        ItShouldShowTheItemInTheWorkspace();

        void ItShouldOpenTheFormWithACompleteSplitToStartFrom()
        {
            panel.ByTestId("single-create-panel").HasAttribute("data-mnl-panel").ShouldBeTrue();
            IElement form = panel.ByTestId("budget-item-form");
            form.GetAttribute("hx-post").ShouldBe(items);
            Guid.TryParse(member, out _).ShouldBeTrue();
            form.ByTestId("payer-split-total").TextContent.ShouldBe("100%");
            form.ByTestId("attribution-split-total").GetAttribute("data-on-target").ShouldBe("true");
            form.QuerySelectorAll("template[data-mnl-rows-template]").Length.ShouldBe(2);
            form.FindByTestId("input-realizedAmount").ShouldBeNull();
        }

        void ItShouldSendTheFormBackWithEveryError()
        {
            invalid.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
            invalidForm.ByTestId("error-month").TextContent.ShouldBe("Month is required (1–12)");
            invalidForm.ByTestId("error-budgetFlow").TextContent.ShouldBe("Budget flow is required");
            invalidForm.ByTestId("error-plannedAmount").TextContent.ShouldBe("Planned amount is required");
            invalidForm.ByTestId("error-payerSplit").TextContent.ShouldBe("Payer split must total 100% (currently 60%)");
            invalidForm.ByTestId("error-attributionSplit").TextContent.ShouldBe("Attribution split must total 100% (currently 80%)");
            invalidForm.ByTestId("payer-split-total").GetAttribute("data-on-target").ShouldBe("false");
            invalidForm.QuerySelectorAll("[data-mnl-rows-list] select[name='attribution']").Length.ShouldBe(2);
        }

        void ItShouldAskForAPersonAndAKnownAttribution()
        {
            noPayerForm.ByTestId("error-payerSplit").TextContent.ShouldBe("Choose a person for every payer row");
            noPayerForm.ByTestId("error-attributionSplit").TextContent.ShouldBe("Choose an attribution for every row");
        }

        void ItShouldSaveCloseThePanelAndAskThePageToReload()
        {
            created.StatusCode.ShouldBe(HttpStatusCode.NoContent);
            created.Triggers().ShouldContain("Budget item created.");
            created.Triggers().ShouldContain("\"mnl:close-panel\":true");
            created.Triggers().ShouldContain("\"items-changed\":true");
        }

        void ItShouldShowWhyASecondItemForTheSameMonthIsRefused()
        {
            again.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
            againForm.ByTestId("form-error").TextContent.ShouldNotBeNullOrWhiteSpace();
        }

        void ItShouldShowTheItemInTheWorkspace()
        {
            IElement row = page.QuerySelector("[data-testid^='item-row-']")!;
            row.ByTestId("item-month").TextContent.ShouldBe("March");
            row.ByTestId("planned-amount").TextContent.ShouldBe("R 2 500.50");
        }
    }

    [Test]
    public async Task GivenABudgetItem_WhenEditingIt()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(NoRedirects, Cancellation);
        Guid budgetId = await UiHtml.CreateBudgetAsync(client, CurrentYear);
        Guid rent = await UiHtml.CreateCategoryAsync(client, budgetId, "Rent", "Expense");
        Guid itemId = await UiHtml.CreateItemAsync(client, budgetId, rent, "Expense", 5, 1000m);
        string item = $"/budgets/{budgetId}/categories/{rent}/items/{itemId}";

        IHtmlDocument panel = await (await client.HtmxAsync(HttpMethod.Get, $"{item}/edit")).DocumentAsync();
        string payer = panel.QuerySelector("[data-mnl-rows-list] select[name='payerUserId']")!.ParentElement!
            .QuerySelector("select")!.GetAttribute("id")!;
        string member = panel.QuerySelector("select[name='payerUserId'] option[value]:not([value=''])")!.GetAttribute("value")!;

        HttpResponseMessage invalid = await client.HtmxAsync(HttpMethod.Put, item,
        [
            new("plannedAmount", "1200"), new("realizedAmount", "abc"),
            new("payerUserId", member), new("payerPercent", "100"),
            new("attribution", "Main"), new("attributionPercent", "100")
        ]);
        IHtmlDocument invalidForm = await invalid.DocumentAsync();

        HttpResponseMessage updated = await client.HtmxAsync(HttpMethod.Put, item,
        [
            new("plannedAmount", "1200"), new("realizedAmount", "1150"), new("spentAmount", "900"),
            new("payerUserId", member), new("payerPercent", "100"),
            new("attribution", "Main"), new("attributionPercent", "60"),
            new("attribution", "Rental"), new("attributionPercent", "40")
        ]);
        IHtmlDocument page = await (await client.GetAsync($"/budgets/{budgetId}?category={rent}", Cancellation)).DocumentAsync();
        IHtmlDocument reopened = await (await client.HtmxAsync(HttpMethod.Get, $"{item}/edit")).DocumentAsync();
        HttpResponseMessage missing = await client.HtmxAsync(HttpMethod.Get, $"/budgets/{budgetId}/categories/{rent}/items/{Guid.NewGuid()}/edit");

        ItShouldOpenTheEditFormWithoutMonthOrFlow();
        ItShouldRejectAnAmountThatIsNotANumber();
        ItShouldSaveTheChanges();
        ItShouldSayWhenTheItemIsGone();

        void ItShouldOpenTheEditFormWithoutMonthOrFlow()
        {
            payer.ShouldNotBeNullOrWhiteSpace();
            IElement form = panel.ByTestId("budget-item-form");
            panel.ByTestId($"edit-panel-{itemId}").TextContent.ShouldContain("Edit May");
            form.GetAttribute("hx-put").ShouldBe(item);
            form.QuerySelector("[name='month']").ShouldBeNull();
            form.QuerySelector("[name='budgetFlow']").ShouldBeNull();
            form.ByTestId("input-plannedAmount").GetAttribute("value").ShouldBe("1000");
            form.ByTestId("btn-save").TextContent.Trim().ShouldBe("Update");
        }

        void ItShouldRejectAnAmountThatIsNotANumber()
        {
            invalid.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
            invalidForm.ByTestId("error-realizedAmount").TextContent.ShouldBe("Enter an amount, such as 1250.50");
            invalidForm.ByTestId("input-plannedAmount").GetAttribute("value").ShouldBe("1200");
        }

        void ItShouldSaveTheChanges()
        {
            updated.StatusCode.ShouldBe(HttpStatusCode.NoContent);
            updated.Triggers().ShouldContain("Budget item updated.");
            IElement row = page.ByTestId($"item-row-{itemId}");
            row.ByTestId("planned-amount").TextContent.ShouldBe("R 1 200.00");
            row.ByTestId("realized-amount").TextContent.ShouldBe("R 1 150.00");
            row.ByTestId("spent-amount").TextContent.ShouldBe("R 900.00");
            reopened.QuerySelectorAll("[data-mnl-rows-list] select[name='attribution']").Length.ShouldBe(2);
            reopened.ByTestId("attribution-split-total").TextContent.ShouldBe("100%");
        }

        void ItShouldSayWhenTheItemIsGone()
        {
            missing.StatusCode.ShouldBe(HttpStatusCode.NoContent);
            missing.Triggers().ShouldContain("That item no longer exists.");
        }
    }

    [Test]
    public async Task GivenABudgetItem_WhenRecordingABillAndAPayment()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(NoRedirects, Cancellation);
        Guid budgetId = await UiHtml.CreateBudgetAsync(client, CurrentYear);
        Guid rent = await UiHtml.CreateCategoryAsync(client, budgetId, "Rent", "Expense");
        Guid itemId = await UiHtml.CreateItemAsync(client, budgetId, rent, "Expense", 5, 1000m);
        string item = $"/budgets/{budgetId}/categories/{rent}/items/{itemId}";

        IHtmlDocument billPanel = await (await client.HtmxAsync(HttpMethod.Get, $"{item}/realize")).DocumentAsync();
        IHtmlDocument paymentPanel = await (await client.HtmxAsync(HttpMethod.Get, $"{item}/spent")).DocumentAsync();
        HttpResponseMessage invalidBill = await client.HtmxAsync(HttpMethod.Put, $"{item}/realize", [new("amount", "0")]);
        IHtmlDocument invalidBillForm = await invalidBill.DocumentAsync();
        HttpResponseMessage invalidPayment = await client.HtmxAsync(HttpMethod.Put, $"{item}/spent", [new("amount", "")]);
        HttpResponseMessage bill = await client.HtmxAsync(HttpMethod.Put, $"{item}/realize", [new("amount", "1100")]);
        HttpResponseMessage payment = await client.HtmxAsync(HttpMethod.Put, $"{item}/spent", [new("amount", "1050.25")]);
        IHtmlDocument page = await (await client.GetAsync($"/budgets/{budgetId}?category={rent}&month=5", Cancellation)).DocumentAsync();

        ItShouldAskForOneAmountInASmallDialog();
        ItShouldInsistOnAPositiveAmount();
        ItShouldRecordBothAndAskThePageToReload();
        ItShouldShowTheNewColumnsInTheWorkspaceAndTheSummary();

        void ItShouldAskForOneAmountInASmallDialog()
        {
            billPanel.ByTestId("amount-panel").GetAttribute("data-mode").ShouldBe("dialog");
            billPanel.ByTestId("amount-form").GetAttribute("hx-put").ShouldBe($"{item}/realize");
            billPanel.QuerySelector("label")!.TextContent.ShouldContain("Bill amount");
            paymentPanel.ByTestId("amount-form").GetAttribute("hx-put").ShouldBe($"{item}/spent");
            paymentPanel.QuerySelector("label")!.TextContent.ShouldContain("Payment amount");
        }

        void ItShouldInsistOnAPositiveAmount()
        {
            invalidBill.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
            invalidBillForm.ByTestId("error-amount").TextContent.ShouldBe("Amount is required and must be positive");
            invalidBillForm.QuerySelector("dialog").ShouldBeNull();
            invalidPayment.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        }

        void ItShouldRecordBothAndAskThePageToReload()
        {
            bill.Triggers().ShouldContain("Bill recorded.");
            payment.Triggers().ShouldContain("Payment recorded.");
            payment.Triggers().ShouldContain("\"items-changed\":true");
        }

        void ItShouldShowTheNewColumnsInTheWorkspaceAndTheSummary()
        {
            IElement row = page.ByTestId($"item-row-{itemId}");
            row.ByTestId("realized-amount").TextContent.ShouldBe("R 1 100.00");
            row.ByTestId("spent-amount").TextContent.ShouldBe("R 1 050.25");
            IElement net = page.ByTestId("summary-net");
            net.ByTestId("summary-realized").TextContent.ShouldBe("-1 100.00");
            net.ByTestId("summary-spent").TextContent.ShouldBe("-1 050.25");
        }
    }

    [Test]
    public async Task GivenABudgetItem_WhenFillingItForward()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(NoRedirects, Cancellation);
        Guid budgetId = await UiHtml.CreateBudgetAsync(client, CurrentYear);
        Guid rent = await UiHtml.CreateCategoryAsync(client, budgetId, "Rent", "Expense");
        Guid itemId = await UiHtml.CreateItemAsync(client, budgetId, rent, "Expense", 10, 1000m);
        string item = $"/budgets/{budgetId}/categories/{rent}/items/{itemId}";

        IHtmlDocument panel = await (await client.HtmxAsync(HttpMethod.Get, $"{item}/fill-forward")).DocumentAsync();
        HttpResponseMessage invalid = await client.HtmxAsync(HttpMethod.Post, $"{item}/fill-forward", [new("amount", "-5")]);
        HttpResponseMessage filled = await client.HtmxAsync(HttpMethod.Post, $"{item}/fill-forward", [new("amount", "1300")]);
        IHtmlDocument page = await (await client.GetAsync($"/budgets/{budgetId}?category={rent}", Cancellation)).DocumentAsync();

        ItShouldOfferThePlannedAmountAndSayWhatWillHappen();
        ItShouldInsistOnAPositiveAmount();
        ItShouldCreateTheRemainingMonths();

        void ItShouldOfferThePlannedAmountAndSayWhatWillHappen()
        {
            IElement form = panel.ByTestId("amount-form");
            form.GetAttribute("hx-post").ShouldBe($"{item}/fill-forward");
            form.ByTestId("input-amount").GetAttribute("value").ShouldBe("1000");
            panel.ByTestId("amount-panel-info").TextContent.ShouldContain("from October through December");
        }

        void ItShouldInsistOnAPositiveAmount() => invalid.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);

        void ItShouldCreateTheRemainingMonths()
        {
            filled.StatusCode.ShouldBe(HttpStatusCode.NoContent);
            filled.Triggers().ShouldContain("Budget item filled forward.");
            page.QuerySelectorAll("[data-testid='item-month']").Select(month => month.TextContent)
                .ShouldBe(["October", "November", "December"]);
        }
    }

    [Test]
    public async Task GivenALeafCategory_WhenAddingLineItemsForEveryMonth()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(NoRedirects, Cancellation);
        Guid budgetId = await UiHtml.CreateBudgetAsync(client, CurrentYear);
        Guid rent = await UiHtml.CreateCategoryAsync(client, budgetId, "Rent", "Expense");
        string items = $"/budgets/{budgetId}/categories/{rent}/items";

        IHtmlDocument panel = await (await client.HtmxAsync(HttpMethod.Get, $"{items}/bulk/new")).DocumentAsync();
        string member = panel.QuerySelector("select[name='payerUserId'] option[selected]")!.GetAttribute("value")!;
        HttpResponseMessage invalid = await client.HtmxAsync(HttpMethod.Post, $"{items}/bulk",
        [
            new("budgetFlow", "Expense"), new("plannedAmount", "0"),
            new("payerUserId", member), new("payerPercent", "100"),
            new("attribution", "Main"), new("attributionPercent", "100")
        ]);
        IHtmlDocument invalidForm = await invalid.DocumentAsync();
        HttpResponseMessage created = await client.HtmxAsync(HttpMethod.Post, $"{items}/bulk",
        [
            new("budgetFlow", "Expense"), new("plannedAmount", "750"),
            new("payerUserId", member), new("payerPercent", "100"),
            new("attribution", "Main"), new("attributionPercent", "100")
        ]);
        IHtmlDocument page = await (await client.GetAsync($"/budgets/{budgetId}?category={rent}", Cancellation)).DocumentAsync();

        ItShouldOpenTheBulkFormWithoutAMonth();
        ItShouldInsistOnAPositiveAmount();
        ItShouldCreateTwelveItems();

        void ItShouldOpenTheBulkFormWithoutAMonth()
        {
            IElement form = panel.ByTestId("bulk-create-form");
            panel.ByTestId("bulk-create-panel").TextContent.ShouldContain("Add line items for all months");
            form.GetAttribute("hx-post").ShouldBe($"{items}/bulk");
            form.QuerySelector("[name='month']").ShouldBeNull();
        }

        void ItShouldInsistOnAPositiveAmount()
        {
            invalid.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
            invalidForm.ByTestId("error-plannedAmount").TextContent.ShouldBe("Amount must be positive");
        }

        void ItShouldCreateTwelveItems()
        {
            created.Triggers().ShouldContain("Budget items created for all 12 months.");
            page.QuerySelectorAll("[data-testid^='item-row-']").Length.ShouldBe(12);
        }
    }

    [Test]
    public async Task GivenABudgetItem_WhenDeletingIt()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory(budgetItemsEnabled: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(NoRedirects, Cancellation);
        Guid budgetId = await UiHtml.CreateBudgetAsync(client, CurrentYear);
        Guid rent = await UiHtml.CreateCategoryAsync(client, budgetId, "Rent", "Expense");
        Guid itemId = await UiHtml.CreateItemAsync(client, budgetId, rent, "Expense", 5, 1000m);
        string item = $"/budgets/{budgetId}/categories/{rent}/items/{itemId}";

        IHtmlDocument panel = await (await client.HtmxAsync(HttpMethod.Get, $"{item}/delete")).DocumentAsync();
        HttpResponseMessage deleted = await client.HtmxAsync(HttpMethod.Delete, item);
        IHtmlDocument page = await (await client.GetAsync($"/budgets/{budgetId}?category={rent}", Cancellation)).DocumentAsync();
        HttpResponseMessage again = await client.HtmxAsync(HttpMethod.Delete, item);
        IHtmlDocument againPanel = await again.DocumentAsync();

        ItShouldAskFirst();
        ItShouldDeleteAndAskThePageToReload();
        ItShouldBringThePanelBackWithTheReasonWhenTheDeleteFails();

        void ItShouldAskFirst()
        {
            IElement dialog = panel.ByTestId("delete-confirmation-panel");
            dialog.GetAttribute("data-mode").ShouldBe("dialog");
            dialog.ByTestId("btn-confirm-no").HasAttribute("data-mnl-close").ShouldBeTrue();
            IElement yes = dialog.ByTestId("btn-confirm-yes");
            yes.GetAttribute("hx-delete").ShouldBe(item);
            yes.GetAttribute("hx-target").ShouldBe("closest dialog");
        }

        void ItShouldDeleteAndAskThePageToReload()
        {
            deleted.StatusCode.ShouldBe(HttpStatusCode.NoContent);
            deleted.Triggers().ShouldContain("Budget item deleted.");
            deleted.Triggers().ShouldContain("\"items-changed\":true");
            page.ByTestId("state-empty").TextContent.ShouldContain("No items for this category yet.");
        }

        void ItShouldBringThePanelBackWithTheReasonWhenTheDeleteFails()
        {
            again.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
            againPanel.ByTestId("delete-error").TextContent.ShouldNotBeNullOrWhiteSpace();
        }
    }
}
