using Menlo.Utilities.Handlers.Water;
using System.Net.Http.Json;

namespace Menlo.Utilities;

[TestSubject(typeof(WaterEndpoints))]
public class WaterEndpointTests
{
    private readonly ClientFactory _factory;

    public WaterEndpointTests(ITestOutputHelper outputHelper)
    {
        _factory = new ClientFactory(outputHelper);
        _factory.AddContainer("WaterReading");
    }

    [Fact]
    public async Task GivenAWaterReadingIsBeingCaptured()
    {
        // Arrange
        HttpClient client = await _factory.CreateClientAsync(CancellationToken.None);
        CaptureWaterReadingCommand command = new(new DateOnly(2024, 8, 2), 16000, new SimpleMoney(1208, "ZAR"));

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/utilities/water/reading", command);

        // Assert
        response.EnsureSuccessStatusCode();
    }
}
