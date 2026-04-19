using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Headers;

namespace Menlo.Api.Tests;

public sealed class SpaProductionHostingTests : TestFixture
{
    [Fact]
    public async Task GetAsync_WithSpaRequestsInProduction_ServesIndexHtml()
    {
        using TestWebApplicationFactory factory = CreateProductionFactory();
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        HttpResponseMessage rootResponse = await client.GetAsync("/", TestContext.Current.CancellationToken);
        string rootBody = await rootResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        HttpResponseMessage indexResponse = await client.GetAsync("/index.html", TestContext.Current.CancellationToken);
        string indexBody = await indexResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        HttpResponseMessage fallbackResponse = await client.GetAsync("/projects/279/details", TestContext.Current.CancellationToken);
        string fallbackBody = await fallbackResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        ItShouldHaveSucceeded(rootResponse);
        ItShouldHaveSucceeded(indexResponse);
        ItShouldHaveSucceeded(fallbackResponse);
        ItShouldServeSpaShell(rootResponse, rootBody);
        ItShouldServeSpaShell(indexResponse, indexBody);
        ItShouldServeSpaShell(fallbackResponse, fallbackBody);
    }

    [Fact]
    public async Task GetAsync_WithOriginHeader_DoesNotIncludeCorsHeaders()
    {
        using TestWebApplicationFactory factory = CreateProductionFactory();
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        HttpResponseMessage apiResponse = await GetWithOriginHeaderAsync(client, "/api/weatherforecast");
        HttpResponseMessage authResponse = await GetWithOriginHeaderAsync(client, "/auth/user");
        HttpResponseMessage healthResponse = await GetWithOriginHeaderAsync(client, "/health");
        HttpResponseMessage spaResponse = await GetWithOriginHeaderAsync(client, "/projects/279/details");

        ItShouldHaveSucceeded(apiResponse);
        ItShouldHaveSucceeded(authResponse);
        ItShouldHaveSucceeded(healthResponse);
        ItShouldHaveSucceeded(spaResponse);
        ItShouldNotIncludeCorsHeaders(apiResponse);
        ItShouldNotIncludeCorsHeaders(authResponse);
        ItShouldNotIncludeCorsHeaders(healthResponse);
        ItShouldNotIncludeCorsHeaders(spaResponse);
    }

    private static TestWebApplicationFactory CreateProductionFactory() =>
        new()
        {
            ContentRootPath = GetSpaHostingContentRoot()
        };

    private static async Task<HttpResponseMessage> GetWithOriginHeaderAsync(HttpClient client, string requestUri)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, requestUri);
        request.Headers.Add("Origin", "https://spa.example.test");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));

        return await client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private static string GetSpaHostingContentRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Menlo.slnx")))
        {
            directory = directory.Parent;
        }

        return directory is null
            ? throw new DirectoryNotFoundException("Could not locate the repository root for SPA hosting test assets.")
            : Path.Combine(directory.FullName, "src", "api", "Menlo.Api.Tests", "TestAssets", "SpaHost");
    }

    private static void ItShouldServeSpaShell(HttpResponseMessage response, string body)
    {
        response.Content.Headers.ContentType?.MediaType.ShouldBe("text/html");
        body.ShouldContain("Menlo SPA Test Shell");
        body.ShouldContain("issue-279");
    }

    private static void ItShouldNotIncludeCorsHeaders(HttpResponseMessage response)
    {
        response.Headers.Contains("Access-Control-Allow-Origin").ShouldBeFalse();
        response.Headers.Contains("Access-Control-Allow-Credentials").ShouldBeFalse();
        response.Headers.Contains("Access-Control-Allow-Headers").ShouldBeFalse();
        response.Headers.Contains("Access-Control-Allow-Methods").ShouldBeFalse();
        response.Headers.Contains("Access-Control-Expose-Headers").ShouldBeFalse();
    }
}
