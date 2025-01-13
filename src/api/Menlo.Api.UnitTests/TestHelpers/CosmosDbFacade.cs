using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Microsoft.Azure.Cosmos;
using System.Diagnostics.CodeAnalysis;
using Testcontainers.CosmosDb;

namespace Menlo.TestHelpers;

public static class CosmosDbFacade
{
    private static CosmosDbContainer? cosmosDbContainer;
    private static readonly object Lock = new();
    private const ushort PortApi = 8081;

    public static string GetConnectionString()
    {
        GuardAgainstNull();

        return cosmosDbContainer.GetConnectionString().Replace("https://", "http://");
    }

    public static async Task InitializeAsync(
        ITestOutputHelper outputHelper,
        string[] containerNames,
        CancellationToken cancellationToken)
    {
        CosmosDbContainer container = CreateContainer(outputHelper, (host, port, cts) => ScaffoldDatabaseAsync(containerNames, host, port, cts));
        await container.StartAsync(cancellationToken);
    }

    private static async Task ScaffoldDatabaseAsync(
        string[] containerNames,
        string host,
        ushort port,
        CancellationToken cancellationToken)
    {
        CosmosClient client = new(
            GetConnectionString(),
            new CosmosClientOptions
            {
                //HttpClientFactory = () => new HttpClient(new UriRewriter(host, port)),
                ConnectionMode = ConnectionMode.Gateway
            });

        Database database = await client.CreateDatabaseIfNotExistsAsync("database", cancellationToken: cancellationToken);
        foreach (string containerName in containerNames)
        {
            await database.CreateContainerIfNotExistsAsync(containerName, "/id", cancellationToken: cancellationToken);
            await database.GetContainer(containerName).ReadContainerAsync(cancellationToken: cancellationToken);
        }
    }

    private static CosmosDbContainer CreateContainer(ITestOutputHelper outputHelper, Func<string, ushort, CancellationToken, Task> areContainersReady)
    {
        if (cosmosDbContainer is not null)
        {
            return cosmosDbContainer;
        }

        lock (Lock)
        {
            if (cosmosDbContainer is not null)
            {
                return cosmosDbContainer;
            }

            var container = new CosmosDbBuilder()
                .WithLogger(outputHelper.ToLogger<CosmosDbContainer>())
                .WithImage("mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:vnext-preview")
                .WithEnvironment("ENABLE_EXPLORER", "false")
                //.WithEnvironment("PROTOCOL", "https")
                //.WithPortBinding(1234, false)
                //.WithPortBinding(PortApi, false)
                .WithWaitStrategy(Wait.ForUnixContainer().AddCustomWaitStrategy(new WaitUntil(areContainersReady)))
                //.WithCleanUp(false)
                .Build();
            return cosmosDbContainer = container;
        }
    }

    [MemberNotNull(nameof(cosmosDbContainer))]
    private static void GuardAgainstNull()
    {
        if (cosmosDbContainer is null)
        {
            throw new InvalidOperationException("The CosmosDB container has not been initialized.");
        }
    }

    private class UriRewriter(string host, ushort port) : DelegatingHandler(new HttpClientHandler())
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            request.RequestUri = new UriBuilder(
                Uri.UriSchemeHttp,
                host,
                port,
                request.RequestUri?.PathAndQuery).Uri;
            return base.SendAsync(request, cancellationToken);
        }
    }

    private sealed class WaitUntil(Func<string, ushort, CancellationToken, Task> areContainersReady) : IWaitUntil
    {
        public async Task<bool> UntilAsync(IContainer container)
        {
            CancellationTokenSource cts = new(TimeSpan.FromSeconds(30));
            string connectionString = GetConnectionString();
            CosmosClient client = new(
                connectionString,
                new CosmosClientOptions
                {
                    ConnectionMode = ConnectionMode.Gateway,
                    HttpClientFactory = () => new HttpClient(new UriRewriter(container.Hostname, container.GetMappedPublicPort(PortApi)))
                });

            try
            {
                using HttpClient httpClient = new(new UriRewriter(container.Hostname, container.GetMappedPublicPort(PortApi)));
                await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://localhost/"), cts.Token);

                AccountProperties? account = await client.ReadAccountAsync();

                if (account is not null)
                {
                    await areContainersReady(container.Hostname, container.GetMappedPublicPort(PortApi), cts.Token);
                    return true;
                }
            }
            catch (HttpRequestException) { return false; }
            catch (CosmosException) { return false; }

            return false;
        }
    }
}
