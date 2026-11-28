using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Text.Json;

namespace Menlo.Api.Tests.Budget;

/// <summary>
/// The shipped budget-item model, its JSON endpoints and its pages are gone. Whichever way the budget flag is set,
/// none of their addresses answer.
/// </summary>
public sealed class RemovedBudgetRoutesTests : TestFixture
{
    private const string Budget = "3f2504e0-4f89-11d3-9a0c-0305e82c3301";
    private const string Category = "6fa459ea-ee8a-3ca4-894e-db77e160355e";
    private const string Item = "9b2e8a4c-1d3f-4e5a-8b6c-7d8e9f0a1b2c";

    // The receipts a member sends for a budget year live under the year, and aren't part of the old budget
    private const string ReceiptPaths = "/api/budgets/{year}/receipts";

    private static CancellationToken Cancellation => TestContext.Current!.Execution.CancellationToken;

    private static IReadOnlyList<(string Method, string Path)> OldRoutes()
    {
        string categories = $"/api/budgets/{Budget}/categories";
        string items = $"{categories}/{Category}/items";
        string pageCategories = $"/budgets/{Budget}/categories";
        string pageItems = $"{pageCategories}/{Category}/items";

        (string, string)[] routes =
        [
            // JSON API
            ("GET", "/api/budgets"),
            ("POST", "/api/budgets/2026"),
            ("GET", $"/api/budgets/{Budget}"),
            ("POST", $"/api/budgets/{Budget}/activate"),
            ("GET", $"/api/budgets/{Budget}/summary"),
            ("GET", categories),
            ("POST", categories),
            ("GET", $"{categories}/{Category}"),
            ("PUT", $"{categories}/{Category}"),
            ("PUT", $"{categories}/{Category}/reparent"),
            ("DELETE", $"{categories}/{Category}"),
            ("PUT", $"{categories}/{Category}/restore"),
            ("GET", items),
            ("POST", items),
            ("POST", $"{items}/bulk"),
            ("POST", $"{items}/fill-forward"),
            ("PUT", $"{items}/{Item}"),
            ("PUT", $"{items}/{Item}/realize"),
            ("PUT", $"{items}/{Item}/spent"),
            ("DELETE", $"{items}/{Item}"),

            // Pages, fragments and forms
            ("GET", "/budgets"),
            ("GET", $"/budgets/{Budget}"),
            ("POST", "/budgets/2026"),
            ("POST", "/home/budget-widget"),
            ("GET", $"{pageCategories}/new"),
            ("GET", $"{pageCategories}/{Category}/edit"),
            ("POST", pageCategories),
            ("PUT", $"{pageCategories}/{Category}"),
            ("DELETE", $"{pageCategories}/{Category}"),
            ("PUT", $"{pageCategories}/{Category}/restore"),
            ("GET", $"{pageItems}/new"),
            ("GET", $"{pageItems}/bulk/new"),
            ("GET", $"{pageItems}/{Item}/edit"),
            ("GET", $"{pageItems}/{Item}/fill-forward"),
            ("GET", $"{pageItems}/{Item}/realize"),
            ("GET", $"{pageItems}/{Item}/spent"),
            ("GET", $"{pageItems}/{Item}/delete"),
            ("POST", pageItems),
            ("POST", $"{pageItems}/bulk"),
            ("PUT", $"{pageItems}/{Item}"),
            ("POST", $"{pageItems}/{Item}/fill-forward"),
            ("PUT", $"{pageItems}/{Item}/realize"),
            ("PUT", $"{pageItems}/{Item}/spent"),
            ("DELETE", $"{pageItems}/{Item}")
        ];

        return routes;
    }

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task GivenTheBudgetFlag_WhenEachOldBudgetRouteIsCalled_ThenNoneAnswers(bool budgetEnabled)
    {
        IReadOnlyList<string> answered = await CallEveryOldRouteAsync(budgetEnabled);

        ItShouldFindNoEndpointForAnyOfThem();

        void ItShouldFindNoEndpointForAnyOfThem() => answered.ShouldBeEmpty(string.Join(", ", answered));
    }

    [Test]
    public async Task GivenTheOpenApiDocument_WhenRead_ThenItHasNoBudgetPaths()
    {
        await using TestWebApplicationFactory factory = new()
        {
            EnvironmentName = Microsoft.Extensions.Hosting.Environments.Development,
            ConfigurationOverrides = new Dictionary<string, string?> { ["Features:Budget"] = "true" }
        };
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/openapi/v1.json", Cancellation);
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Cancellation));
        List<string> budgetPaths = document.RootElement.GetProperty("paths").EnumerateObject()
            .Select(path => path.Name)
            .Where(path => path.StartsWith("/api/budgets", StringComparison.Ordinal))
            .Where(path => !path.StartsWith(ReceiptPaths, StringComparison.Ordinal))
            .ToList();

        ItShouldServeTheDocument();
        ItShouldNotListAnyOldBudgetPath();

        void ItShouldServeTheDocument() => response.StatusCode.ShouldBe(HttpStatusCode.OK);

        void ItShouldNotListAnyOldBudgetPath() => budgetPaths.ShouldBeEmpty(string.Join(", ", budgetPaths));
    }

    // Each route that an endpoint answered, as "METHOD path: status". Unknown API addresses get a bare 404, and so does
    // a GET of an unknown page. A write to an unknown page gets 405, as at any unknown address: the not-found page is
    // GET only, so the sign-in callback stays unrouted. Either way no endpoint is there.
    private static async Task<IReadOnlyList<string>> CallEveryOldRouteAsync(bool budgetEnabled)
    {
        await using TestWebApplicationFactory factory = new()
        {
            ConfigurationOverrides = new Dictionary<string, string?> { ["Features:Budget"] = budgetEnabled ? "true" : "false" }
        };

        // A valid anti-forgery token, so a write is turned away for its address and not for a missing token
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false },
            Cancellation);

        List<string> answered = [];
        foreach ((string method, string path) in OldRoutes())
        {
            HttpResponseMessage response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), path), Cancellation);
            bool pageWrite = method != "GET" && !path.StartsWith("/api/", StringComparison.Ordinal);
            bool nothingThere = response.StatusCode == HttpStatusCode.NotFound
                                || (pageWrite && response.StatusCode == HttpStatusCode.MethodNotAllowed);
            if (!nothingThere)
            {
                answered.Add($"{method} {path}: {(int)response.StatusCode}");
            }
        }

        return answered;
    }
}
