using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Menlo.Api.Tests;

public class ApiSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ApiSmokeTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task WeatherForecast_Endpoint_Returns_Ok()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/weatherforecast");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
