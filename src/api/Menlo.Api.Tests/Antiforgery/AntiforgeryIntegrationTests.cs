using Menlo.Api.Budget;
using Menlo.Api.Tests.Budget;
using Menlo.Lib.Common.ValueObjects;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Text.Json;

namespace Menlo.Api.Tests.Antiforgery;

[Collection("Budget")]
public sealed class AntiForgeryIntegrationTests(BudgetApiFixture fixture) : TestFixture
{
    private const string RequestTokenCookieName = "XSRF-TOKEN";
    private const string HeaderName = "X-XSRF-TOKEN";

    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNameCaseInsensitive = true };

    private static readonly HouseholdId MissingTokenHousehold =
        new(Guid.Parse("c0c0c0c0-c0c0-c0c0-c0c0-c0c0c0c0c0c0"));

    private static readonly HouseholdId ValidTokenHousehold =
        new(Guid.Parse("c1c1c1c1-c1c1-c1c1-c1c1-c1c1c1c1c1c1"));

    [Fact]
    public async Task GetAsync_WithAuthenticatedApiRequestWithoutAntiForgeryToken()
    {
        await using TestWebApplicationFactory factory = new();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response =
            await client.GetAsync("/api/weatherforecast", TestContext.Current.CancellationToken);

        ItShouldHaveSucceeded(response);
    }

    [Fact]
    public async Task PostAsync_WithAuthenticatedBudgetMutationWithoutAntiForgeryToken()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(MissingTokenHousehold);
        using HttpClient client = factory.CreateClient();
        int year = DateTimeOffset.UtcNow.Year;

        HttpResponseMessage response =
            await client.PostAsync($"/api/budgets/{year}", null, TestContext.Current.CancellationToken);

        string problemDetails = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        ItShouldHaveReturned400BadRequest(response);
        ItShouldDescribeTheAntiforgeryFailure(problemDetails);
    }

    [Fact]
    public async Task PostAsync_WithAuthenticatedBudgetMutationAndValidAntiForgeryToken()
    {
        await using BudgetTestWebApplicationFactory factory = CreateIsolatedFactory(ValidTokenHousehold);
        using HttpClient client = factory.CreateClient();
        int year = DateTimeOffset.UtcNow.Year;

        HttpResponseMessage tokenResponse =
            await client.GetAsync("/api/weatherforecast", TestContext.Current.CancellationToken);
        string requestToken = GetRequestToken(tokenResponse);
        client.DefaultRequestHeaders.Add(HeaderName, requestToken);

        HttpResponseMessage response =
            await client.PostAsync($"/api/budgets/{year}", null, TestContext.Current.CancellationToken);
        BudgetDto? dto = await DeserializeBudgetDto(response);

        ItShouldHaveSucceeded(tokenResponse);
        ItShouldHaveReturned201Created(response);
        ItShouldHaveTheRequestedBudgetYear(dto, year);
    }

    [Fact]
    public async Task GetAsync_OnApiEndpoint()
    {
        await using TestWebApplicationFactory factory = new();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response =
            await client.GetAsync("/api/weatherforecast", TestContext.Current.CancellationToken);

        string requestTokenCookie = GetCookieHeader(response, RequestTokenCookieName);

        ItShouldHaveSucceeded(response);
        ItShouldSetAReadableRequestTokenCookie(requestTokenCookie);
    }

    [Fact]
    public async Task PostAsync_OnAuthEndpointWithoutAntiForgeryToken()
    {
        await using TestWebApplicationFactory factory = new();
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        HttpResponseMessage response =
            await client.PostAsync("/auth/logout", null, TestContext.Current.CancellationToken);

        ItShouldNotHaveBeenRejectedByAntiforgery(response);
        ItShouldRedirect(response);
    }

    private BudgetTestWebApplicationFactory CreateIsolatedFactory(HouseholdId householdId) =>
        new(householdId)
        {
            MenloConnectionString = fixture.ConnectionString,
            SkipMigration = true,
            ConfigurationOverrides = new Dictionary<string, string?>
            {
                ["Features:Budget"] = "true"
            }
        };

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
        (response.StatusCode == HttpStatusCode.Found
            || response.StatusCode == HttpStatusCode.Redirect
            || response.StatusCode == HttpStatusCode.SeeOther
            || response.StatusCode == HttpStatusCode.TemporaryRedirect)
            .ShouldBeTrue();
    }

    private static string GetRequestToken(HttpResponseMessage response) =>
        GetCookieHeader(response, RequestTokenCookieName)[($"{RequestTokenCookieName}=").Length..].Split(';', 2)[0];

    private static string GetCookieHeader(HttpResponseMessage response, string cookieName)
    {
        response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? cookieHeaders).ShouldBeTrue();
        cookieHeaders.ShouldNotBeNull();

        return cookieHeaders.Single(header =>
            header.StartsWith($"{cookieName}=", StringComparison.Ordinal));
    }

    private static async Task<BudgetDto?> DeserializeBudgetDto(HttpResponseMessage response)
    {
        string content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        return JsonSerializer.Deserialize<BudgetDto>(content, JsonOptions);
    }
}
