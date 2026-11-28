using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;

namespace Menlo.Api.Tests.Antiforgery;

public sealed class AntiForgeryIntegrationTests : TestFixture
{
    private const string RequestTokenCookieName = "XSRF-TOKEN";
    private const string HeaderName = "X-XSRF-TOKEN";

    // The check runs before routing, so a write to any API address shows it, whether or not an endpoint answers
    private const string ApiWriteUrl = "/api/ai/health";

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
    public async Task PostAsync_WithAuthenticatedApiWriteWithoutAntiForgeryToken()
    {
        await using TestWebApplicationFactory factory = new();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client
            .PostAsync(ApiWriteUrl, null, TestContext.Current!.Execution.CancellationToken);

        string problemDetails = await response.Content.ReadAsStringAsync(TestContext.Current!.Execution.CancellationToken);

        ItShouldHaveReturned400BadRequest(response);
        ItShouldDescribeTheAntiforgeryFailure(problemDetails);
    }

    [Test]
    public async Task PostAsync_WithAuthenticatedApiWriteAndValidAntiForgeryToken()
    {
        await using TestWebApplicationFactory factory = new();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage tokenResponse = await client
            .GetAsync("/api/ai/health", TestContext.Current!.Execution.CancellationToken);
        string requestToken = GetRequestToken(tokenResponse);
        client.DefaultRequestHeaders.Add(HeaderName, requestToken);

        HttpResponseMessage response =
            await client.PostAsync(ApiWriteUrl, null, TestContext.Current!.Execution.CancellationToken);

        ItShouldHaveSucceeded(tokenResponse);
        ItShouldNotHaveBeenRejectedByAntiforgery(response);
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

    private static void ItShouldDescribeTheAntiforgeryFailure(string problemDetails)
    {
        problemDetails.ShouldContain("Invalid anti-forgery token.");
        problemDetails.ShouldContain(HeaderName);
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
}
