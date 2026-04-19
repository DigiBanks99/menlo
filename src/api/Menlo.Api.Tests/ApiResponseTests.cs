using Menlo.AI.Interfaces;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Menlo.Api.Tests;

public sealed class ApiResponseTests(TestWebApplicationFactory factory) : TestFixture, IClassFixture<TestWebApplicationFactory>
{
    [Fact]
    public async Task GivenAuthenticatedUser_WhenCheckingAiHealth_ThenResponseHasExpectedStructure()
    {
        HttpClient client = factory.CreateClient();
        HttpResponseMessage response = await client.GetAsync("/api/ai/health", TestContext.Current.CancellationToken);
        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);

        ItShouldHaveSucceeded(response);
        ItShouldHaveHealthyStatus(body);
        ItShouldHaveAiResponse(body);
    }

    [Fact]
    public async Task GivenAiServiceThrows_WhenCheckingAiHealth_ThenReturnsProblemDetails()
    {
        IChatService throwingChatService = Substitute.For<IChatService>();
        throwingChatService
            .GetResponseAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<string>(new InvalidOperationException("AI unavailable")));

        using WebApplicationFactory<Program> throwingFactory = factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            {
                ServiceDescriptor? existing = services.FirstOrDefault(d => d.ServiceType == typeof(IChatService));
                if (existing is not null) services.Remove(existing);
                services.AddSingleton(throwingChatService);
            }));
        using HttpClient client = throwingFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        HttpResponseMessage response = await client.GetAsync("/api/ai/health", TestContext.Current.CancellationToken);

        ItShouldHaveReturnedInternalServerError(response);
        ItShouldHaveProblemDetailsContentType(response);
    }

    private static void ItShouldHaveHealthyStatus(JsonElement body)
    {
        body.GetProperty("status").GetString().ShouldBe("Healthy");
    }

    private static void ItShouldHaveAiResponse(JsonElement body)
    {
        body.GetProperty("response").GetString().ShouldNotBeNullOrWhiteSpace();
    }

    private static void ItShouldHaveReturnedInternalServerError(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
    }

    private static void ItShouldHaveProblemDetailsContentType(HttpResponseMessage response)
    {
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }

}
