using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Menlo.Api.Budget.Categories;
using Menlo.Api.Budget.Items;
using System.Net.Http.Json;
using System.Text.Json;

namespace Menlo.Api.Tests.Ui;

/// <summary>
/// Helpers for tests of the HTML UI: reading a response as a document, sending the requests htmx
/// sends, and setting up budget data through the JSON API.
/// </summary>
internal static class UiHtml
{
    private static CancellationToken Cancellation => TestContext.Current!.Execution.CancellationToken;

    /// <summary>Parses a response body as HTML.</summary>
    public static async Task<IHtmlDocument> DocumentAsync(this HttpResponseMessage response)
    {
        string html = await response.Content.ReadAsStringAsync(Cancellation);
        IBrowsingContext context = BrowsingContext.New(Configuration.Default);
        return (IHtmlDocument)await context.OpenAsync(request => request.Content(html));
    }

    /// <summary>The first element with a <c>data-testid</c>. Fails the test when there is none.</summary>
    public static IElement ByTestId(this IParentNode node, string testId) =>
        node.QuerySelector($"[data-testid='{testId}']")
        ?? throw new ShouldAssertException($"No element has data-testid '{testId}'.");

    /// <summary>The first element with a <c>data-testid</c>, or null.</summary>
    public static IElement? FindByTestId(this IParentNode node, string testId) =>
        node.QuerySelector($"[data-testid='{testId}']");

    /// <summary>Sends a request the way htmx sends one for a fragment, with an optional form body.</summary>
    public static Task<HttpResponseMessage> HtmxAsync(
        this HttpClient client,
        HttpMethod method,
        string url,
        IEnumerable<KeyValuePair<string, string>>? form = null)
    {
        HttpRequestMessage request = new(method, url);
        request.Headers.Add("HX-Request", "true");
        if (form is not null)
        {
            request.Content = new FormUrlEncodedContent(form);
        }

        return client.SendAsync(request, Cancellation);
    }

    /// <summary>The value of the HX-Trigger response header, or an empty string.</summary>
    public static string Triggers(this HttpResponseMessage response) =>
        response.Headers.TryGetValues("HX-Trigger", out IEnumerable<string>? values) ? string.Join(',', values) : string.Empty;

    public static async Task<Guid> CreateBudgetAsync(HttpClient client, int year)
    {
        HttpResponseMessage response = await client.PostAsync($"/api/budgets/{year}", null, Cancellation);
        return await IdOfAsync(response);
    }

    public static async Task<Guid> CreateCategoryAsync(
        HttpClient client,
        Guid budgetId,
        string name,
        string budgetFlow,
        Guid? parentId = null,
        string? attribution = null)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories",
            new CreateCategoryRequest(name, budgetFlow, parentId, Attribution: attribution),
            Cancellation);
        return await IdOfAsync(response);
    }

    public static async Task<Guid> CreateItemAsync(
        HttpClient client,
        Guid budgetId,
        Guid categoryId,
        string budgetFlow,
        int month,
        decimal amount)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/budgets/{budgetId}/categories/{categoryId}/items",
            new CreateBudgetItemRequest(
                month,
                budgetFlow,
                amount,
                "ZAR",
                [new PayerAllocationDto(Guid.NewGuid(), 100)],
                [new AttributionAllocationDto("Main", 100)]),
            Cancellation);
        return await IdOfAsync(response);
    }

    private static async Task<Guid> IdOfAsync(HttpResponseMessage response)
    {
        string content = await response.Content.ReadAsStringAsync(Cancellation);
        response.IsSuccessStatusCode.ShouldBeTrue($"Test data setup failed: {content}");
        return JsonDocument.Parse(content).RootElement.GetProperty("id").GetGuid();
    }
}
