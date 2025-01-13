using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace Menlo.TestHelpers;

internal static class WebApplicationFactoryExtensions
{
    internal static WebApplicationFactory<Program> ConfigureMenloDefaults(
        this WebApplicationFactory<Program> factory,
        ITestOutputHelper outputHelper,
        string cosmosConnectionString)
    {
        return factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureLogging(logBuilder => logBuilder.ClearProviders().AddXUnit(outputHelper));
            builder.UseEnvironment("Development");
            builder.UseSetting("RepositoryOptions:CosmosConnectionString", cosmosConnectionString);
            builder.UseSetting("RepositoryOptions:UseTokenCredential", "false");
        });
    }
}
