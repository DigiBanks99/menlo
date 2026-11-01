using Menlo.Api.Budget;
using Menlo.Api.Tests.Budget;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Text.Json;

namespace Menlo.Api.Tests.Antiforgery;

[ClassDataSource<BudgetApiFixture>(Shared = SharedType.PerTestSession)]
[NotInParallel("Budget")]
public sealed class AntiForgeryIntegrationTests(BudgetApiFixture fixture) : TestFixture
{
    private const string RequestTokenCookieName = "XSRF-TOKEN";
    private const string HeaderName = "X-XSRF-TOKEN";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [Test]
    public async Task GetAsync_WithAuthenticatedApiRequestWithoutAntiForgeryToken()
    {
        await using TestWebApplicationFactory factory = new();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client
            .GetAsync("/api/ai/health", TestContext.Current!.Execution.CancellationToken);

        ItShouldHaveSucceeded(response);
    }

    [Test]
    public async Task PostAsync_WithAuthenticatedBudgetMutationWithoutAntiForgeryToken()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory();
        using HttpClient client = factory.CreateClient();
        int year = DateTimeOffset.UtcNow.Year;

        HttpResponseMessage response = await client
            .PostAsync($"/api/budgets/{year}", null, TestContext.Current!.Execution.CancellationToken);

        string problemDetails = await response.Content.ReadAsStringAsync(TestContext.Current!.Execution.CancellationToken);

        ItShouldHaveReturned400BadRequest(response);
        ItShouldDescribeTheAntiforgeryFailure(problemDetails);
    }

    [Test]
    public async Task PostAsync_WithAuthenticatedBudgetMutationAndValidAntiForgeryToken()
    {
        await using BudgetTestWebApplicationFactory factory = fixture.CreateIsolatedFactory();
        using HttpClient client = factory.CreateClient();
        int year = DateTimeOffset.UtcNow.Year;

        HttpResponseMessage tokenResponse = await client
            .GetAsync("/api/ai/health", TestContext.Current!.Execution.CancellationToken);
        string requestToken = GetRequestToken(tokenResponse);
        client.DefaultRequestHeaders.Add(HeaderName, requestToken);

        HttpResponseMessage response =
            await client.PostAsync($"/api/budgets/{year}", null, TestContext.Current!.Execution.CancellationToken);
        BudgetDto? dto = await DeserializeBudgetDto(response);

        ItShouldHaveSucceeded(tokenResponse);
        ItShouldHaveReturned201Created(response);
        ItShouldHaveTheRequestedBudgetYear(dto, year);
    }

    [Test]
    public async Task GetAsync_OnApiEndpoint()
    {
        await using TestWebApplicationFactory factory = new();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client
            .GetAsync("/api/ai/health", TestContext.Current!.Execution.CancellationToken);

        string requestTokenCookie = GetCookieHeader(response, RequestTokenCookieName);

        ItShouldHaveSucceeded(response);
        ItShouldSetAReadableRequestTokenCookie(requestTokenCookie);
    }

    [Test]
    public async Task PostAsync_OnAuthEndpointWithoutAntiForgeryToken()
    {
        await using TestWebApplicationFactory factory = new();
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        HttpResponseMessage response = await client
            .PostAsync("/auth/logout", null, TestContext.Current!.Execution.CancellationToken);

        ItShouldNotHaveBeenRejectedByAntiforgery(response);
        ItShouldRedirect(response);
    }

    private static void ItShouldHaveReturned400BadRequest(HttpResponseMessage response) =>
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

    private static void ItShouldHaveReturned201Created(HttpResponseMessage response) =>
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

    private static void ItShouldDescribeTheAntiforgeryFailure(string problemDetails)
    {
        problemDetails.ShouldContain("Invalid anti-forgery token.");
        problemDetails.ShouldContain(HeaderName);
    }

    private static void ItShouldHaveTheRequestedBudgetYear(BudgetDto? dto, int expectedYear)
    {
        dto.ShouldNotBeNull();
        dto.Year.ShouldBe(expectedYear);
    }

    private static void ItShouldSetAReadableRequestTokenCookie(string requestTokenCookie)
    {
        requestTokenCookie.ShouldStartWith($"{RequestTokenCookieName}=");
        requestTokenCookie.ShouldNotContain("HttpOnly");
    }

    private static void ItShouldNotHaveBeenRejectedByAntiforgery(HttpResponseMessage response) =>
        response.StatusCode.ShouldNotBe(HttpStatusCode.BadRequest);

    private static void ItShouldRedirect(HttpResponseMessage response)
    {
        response.Headers.Location.ShouldNotBeNull();
        (response.StatusCode is HttpStatusCode.Found
                or HttpStatusCode.Redirect
                or HttpStatusCode.SeeOther
                or HttpStatusCode.TemporaryRedirect)
            .ShouldBeTrue();
    }

    private static string GetRequestToken(HttpResponseMessage response) =>
        GetCookieHeader(response, RequestTokenCookieName)[($"{RequestTokenCookieName}=").Length..].Split(';', 2)[0];

    private static string GetCookieHeader(HttpResponseMessage response, string cookieName)
    {
        response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? cookieHeaders).ShouldBeTrue();
        string[] headers = cookieHeaders as string[] ?? cookieHeaders.ToArray();
        headers.ShouldNotBeNull();

        return headers.Single(header => header.StartsWith($"{cookieName}=", StringComparison.Ordinal));
    }

    private static async Task<BudgetDto?> DeserializeBudgetDto(HttpResponseMessage response)
    {
        string content = await response.Content.ReadAsStringAsync(TestContext.Current!.Execution.CancellationToken);
        return JsonSerializer.Deserialize<BudgetDto>(content, JsonOptions);
    }
}
