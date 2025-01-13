using Testcontainers.CosmosDb;

namespace Menlo.TestHelpers;

[UsedImplicitly]
public sealed class ClientFactory(ITestOutputHelper outputHelper)
{
    private ISet<string> _containers = new HashSet<string>();

    public void AddContainer(string containerName)
    {
        _containers.Add(containerName);
    }

    public async Task<HttpClient> CreateClientAsync(CancellationToken cancellationToken)
    {
        await CosmosDbFacade.InitializeAsync(outputHelper, _containers.ToArray(), cancellationToken);

        outputHelper.WriteLine("Connection string: {0}", CosmosDbFacade.GetConnectionString());
        return Factory
            .ConfigureMenloDefaults(outputHelper, CosmosDbFacade.GetConnectionString())
            .CreateClient();
    }

    private static readonly WebApplicationFactory<Program> Factory = new();
}
